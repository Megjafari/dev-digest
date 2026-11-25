using System.Text.Json;
using System.Text.RegularExpressions;

static class JobSearch
{
    const string Platsbanken = "arbetsformedlingen.se";

    // JobTech taxonomy ids: the municipality of Göteborg and the occupation field "Data/IT"
    const string Filters = "&municipality=PVZL_BQT_XtL&occupation-field=apaJ_2ja_LuF";

    /// <summary>Ads published on Platsbanken.</summary>
    public static Task<List<Item>> PlatsbankenAsync(HttpClient http, string query) =>
        SearchAsync(http, "Platsbanken", query,
            $"https://jobsearch.api.jobtechdev.se/search?limit=20&sort=pubdate-desc{Filters}&q={Uri.EscapeDataString(query)}",
            h => new Item(Str(h, "employer", "name"), Str(h, "headline"), Str(h, "webpage_url"), Str(h, "description", "text")));

    /// <summary>
    /// JobTech Links: ads collected from several Swedish job sites, not only Platsbanken.
    /// The Platsbanken link is preferred when an ad has one, so the seen list drops ads already
    /// found through <see cref="PlatsbankenAsync"/>. Other sites are named after the employer.
    /// </summary>
    public static Task<List<Item>> LinksAsync(HttpClient http, string query) =>
        SearchAsync(http, "JobTech Links", query,
            $"https://links.api.jobtechdev.se/joblinks?limit=20&sort=pubdate-desc{Filters}&q={Uri.EscapeDataString(query)}",
            h =>
            {
                var links = new List<(string Site, string Url)>();
                if (h.TryGetProperty("source_links", out var l) && l.ValueKind == JsonValueKind.Array)
                    links.AddRange(l.EnumerateArray().Select(x => (Str(x, "label"), Str(x, "url"))).Where(x => x.Item2.Length > 0));

                var (site, url) = links.Any(x => x.Site == Platsbanken)
                    ? links.First(x => x.Site == Platsbanken)
                    : links.FirstOrDefault(("", ""));
                var employer = Str(h, "employer", "name");
                return new Item(site == Platsbanken || site.Length == 0 ? employer : $"{employer}, via {site}", Str(h, "headline"), url, Str(h, "brief"));
            });

    /// <summary>True if the headline has one of <paramref name="words"/> as a whole word, ignoring case.</summary>
    internal static bool IsSenior(Item job, IEnumerable<string> words) =>
        words.Any(w => Regex.IsMatch(job.Title, $@"(?<!\w){Regex.Escape(w)}(?!\w)", RegexOptions.IgnoreCase));

    /// <summary>True if the headline says the ad is for LIA, praktik, an internship or a trainee program.</summary>
    internal static bool IsInternship(Item job) =>
        Regex.IsMatch(job.Title, @"\bLIA\b|\bpraktik|\btrainee|\binternship|\bexamensarbete", RegexOptions.IgnoreCase);

    /// <summary>
    /// The most years of experience the ad text asks for, such as "5 års erfarenhet" or "3+ years of experience".
    /// For a range like "2-4 år" the lower number counts. Returns 0 if the text names no number of years.
    /// </summary>
    internal static int YearsRequired(Item job) =>
        Regex.Matches(job.Snippet,
                @"(\d{1,2})\s*(?:[-–]\s*\d{1,2}\s*)?\+?\s*(?:års|år|years?|yrs)\b(?:\W+\w+){0,4}?\W+(?:erfarenhet|experience)",
                RegexOptions.IgnoreCase)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

    /// <summary>
    /// Takes the first result of every list, then the second of every list, and so on,
    /// so that each search gets a fair share of the daily limit instead of the first one taking it all.
    /// </summary>
    internal static IEnumerable<Item> Interleave(IReadOnlyList<List<Item>> lists)
    {
        for (var i = 0; lists.Any(l => i < l.Count); i++)
            foreach (var list in lists)
                if (i < list.Count) yield return list[i];
    }

    static async Task<List<Item>> SearchAsync(HttpClient http, string name, string query, string url, Func<JsonElement, Item> toItem)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("accept", "application/json");

            using var resp = await http.SendAsync(req, cts.Token);
            resp.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);

            var jobs = doc.RootElement.GetProperty("hits").EnumerateArray()
                .Select(toItem)
                .Where(i => i.Title.Length > 0 && i.Link.Length > 0)
                .ToList();

            Console.WriteLine($"{name} '{query}': {jobs.Count} hits");
            return jobs;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{name} failed for '{query}': {e.Message}");
            return [];
        }
    }

    static string Str(JsonElement e, params string[] path)
    {
        foreach (var key in path)
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(key, out e)) return "";
        return e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "";
    }
}
