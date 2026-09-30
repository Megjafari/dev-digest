using System.Text.Json;

static class JobSearch
{
    public static async Task<List<Item>> SearchAsync(HttpClient http, string query)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var url = $"https://jobsearch.api.jobtechdev.se/search?limit=20&sort=pubdate-desc&q={Uri.EscapeDataString(query)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("accept", "application/json");

            using var resp = await http.SendAsync(req, cts.Token);
            resp.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);

            var jobs = doc.RootElement.GetProperty("hits").EnumerateArray()
                .Select(h => new Item(Str(h, "employer", "name"), Str(h, "headline"), Str(h, "webpage_url"), ""))
                .Where(i => i.Title.Length > 0 && i.Link.Length > 0)
                .ToList();

            Console.WriteLine($"Jobs '{query}': {jobs.Count} hits");
            return jobs;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Jobs failed for '{query}': {e.Message}");
            return [];
        }
    }

    static string Str(JsonElement e, params string[] path)
    {
        foreach (var key in path)
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(key, out e)) return "";
        return e.GetString() ?? "";
    }
}