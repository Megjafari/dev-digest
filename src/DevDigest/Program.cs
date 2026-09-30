using System.Net.Http.Json;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Xml;

// DRY_RUN=1 writes the digest to digests-preview/ and does NOT update the "seen" list,
// so local testing never interferes with what the daily workflow sends.
var dryRun = Environment.GetEnvironmentVariable("DRY_RUN") == "1";
var outDir = dryRun ? "digests-preview" : "digests";
const string SeenPath = "digests/seen.json";

var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("dev-digest/1.0 (+https://github.com/Megjafari)");

var conflictKeywords = new[]
{
    "iran", "tehran", "israel", "gaza", "palestin", "hamas", "hezbollah",
    "west bank", "lebanon", "houthi", "idf", "netanyahu", "khamenei",
};

var feeds = new (string Section, string Name, string Url, string[]? Keywords)[]
{
    // Krig och konflikter (filtreras pa nyckelord, blandade perspektiv)
    ("Krig och konflikter", "Al Jazeera", "https://www.aljazeera.com/xml/rss/all.xml", conflictKeywords),
    ("Krig och konflikter", "BBC Middle East", "https://feeds.bbci.co.uk/news/world/middle_east/rss.xml", conflictKeywords),
    ("Krig och konflikter", "The Guardian", "https://www.theguardian.com/world/middleeast/rss", conflictKeywords),
    ("Krig och konflikter", "Times of Israel", "https://www.timesofisrael.com/feed/", conflictKeywords),

    // DevOps och software development
    ("DevOps och software development", "Hacker News", "https://hnrss.org/frontpage", null),
    ("DevOps och software development", "dev.to devops", "https://dev.to/feed/tag/devops", null),
    ("DevOps och software development", "CNCF", "https://www.cncf.io/feed/", null),
    ("DevOps och software development", "Docker Blog", "https://www.docker.com/blog/feed/", null),
    ("DevOps och software development", "Kubernetes", "https://kubernetes.io/feed.xml", null),
    ("DevOps och software development", ".NET Blog", "https://devblogs.microsoft.com/dotnet/feed/", null),
    ("DevOps och software development", "Azure Blog", "https://azure.microsoft.com/en-us/blog/feed/", null),
    ("DevOps och software development", "GitHub Changelog", "https://github.blog/changelog/feed/", null),
    ("DevOps och software development", "PostgreSQL", "https://www.postgresql.org/news.rss", null),

    // AI i varlden
    ("AI i världen", "Hacker News AI", "https://hnrss.org/newest?q=LLM&points=100", null),
    ("AI i världen", "Simon Willison", "https://simonwillison.net/atom/everything/", null),
    ("AI i världen", "The Verge AI", "https://www.theverge.com/rss/ai-artificial-intelligence/index.xml", null),
    ("AI i världen", "MIT Technology Review", "https://www.technologyreview.com/topic/artificial-intelligence/feed", null),
};

var jobQueries = new[]
{
    "devops göteborg",
    ".NET utvecklare göteborg",
    "molnutvecklare azure",
    "junior systemutvecklare",
};

var now = DateTime.UtcNow;
var today = now.ToString("yyyy-MM-dd");
var seen = LoadSeen();

var sb = new StringBuilder();
sb.AppendLine($"# Digest {today}");
sb.AppendLine();

// ---------- Nyheter ----------
sb.AppendLine("## Nyheter");
sb.AppendLine();

var newsCount = 0;
foreach (var group in feeds.GroupBy(f => f.Section))
{
    var groupSb = new StringBuilder();
    foreach (var feed in group)
    {
        var candidates = await ReadFeed(feed.Url, feed.Keywords);
        var fresh = new List<(string Title, string Link)>();
        foreach (var item in candidates)
        {
            if (fresh.Count >= 4) break;
            if (seen.ContainsKey(item.Link)) continue;
            seen[item.Link] = today;
            fresh.Add(item);
        }
        if (fresh.Count == 0) continue;

        groupSb.AppendLine($"**{feed.Name}**");
        foreach (var (title, link) in fresh)
            groupSb.AppendLine($"* [{title}]({link})");
        groupSb.AppendLine();
        newsCount += fresh.Count;
    }

    if (groupSb.Length > 0)
    {
        sb.AppendLine($"### {group.Key}");
        sb.AppendLine();
        sb.Append(groupSb);
    }
}
if (newsCount == 0)
{
    sb.AppendLine("Inga nya nyheter idag.");
    sb.AppendLine();
}

// ---------- Jobb ----------
sb.AppendLine("## Nya jobbannonser");
sb.AppendLine();

var newJobs = new List<(string Headline, string Employer, string Url)>();
foreach (var q in jobQueries)
{
    foreach (var job in await ReadJobs(q))
    {
        if (newJobs.Count >= 15) break;
        if (seen.ContainsKey(job.Url)) continue;
        seen[job.Url] = today;
        newJobs.Add(job);
    }
}

if (newJobs.Count == 0)
{
    sb.AppendLine("Inga nya annonser idag.");
}
else
{
    foreach (var (headline, employer, url) in newJobs)
        sb.AppendLine($"* [{headline}]({url}) ({employer})");
}
sb.AppendLine();

// ---------- Spara ----------
var markdown = sb.ToString();

Directory.CreateDirectory(outDir);
var path = Path.Combine(outDir, $"{today}.md");
await File.WriteAllTextAsync(path, markdown);
Console.WriteLine($"Wrote {path} ({newsCount} news, {newJobs.Count} jobs)");

if (!dryRun) SaveSeen(seen);

// ---------- Discord ----------
var webhook = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK");
if (!string.IsNullOrWhiteSpace(webhook))
{
    foreach (var chunk in SplitForDiscord(markdown, 1900))
    {
        // flags = 4 turns off link previews so the channel does not fill with cards
        var resp = await http.PostAsJsonAsync(webhook, new { content = chunk, flags = 4 });
        Console.WriteLine($"Discord: {(int)resp.StatusCode}");
        await Task.Delay(1000);
    }
}
else
{
    Console.WriteLine("DISCORD_WEBHOOK not set, skipping delivery");
}

// ---------- Funktioner ----------
async Task<List<(string Title, string Link)>> ReadFeed(string url, string[]? keywords)
{
    try
    {
        await using var stream = await http.GetStreamAsync(url);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var feed = SyndicationFeed.Load(reader);

        var items = feed.Items
            .OrderByDescending(i => i.PublishDate == default ? i.LastUpdatedTime : i.PublishDate)
            .Take(40);

        var result = new List<(string, string)>();
        foreach (var i in items)
        {
            var title = (i.Title?.Text ?? "(utan titel)").Trim();
            var link = i.Links.FirstOrDefault()?.Uri.ToString() ?? "";
            if (link.Length == 0) continue;

            if (keywords is not null)
            {
                var haystack = (title + " " + (i.Summary?.Text ?? "")).ToLowerInvariant();
                if (!keywords.Any(k => haystack.Contains(k))) continue;
            }

            result.Add((title.Replace("[", "(").Replace("]", ")"), link));
        }
        return result;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Feed failed {url}: {e.Message}");
        return new();
    }
}

async Task<List<(string Headline, string Employer, string Url)>> ReadJobs(string query)
{
    try
    {
        var url = "https://jobsearch.api.jobtechdev.se/search?limit=20&sort=pubdate-desc&q=" + Uri.EscapeDataString(query);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("accept", "application/json");
        using var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        var result = new List<(string, string, string)>();
        foreach (var hit in doc.RootElement.GetProperty("hits").EnumerateArray())
        {
            var headline = hit.TryGetProperty("headline", out var h) ? h.GetString() ?? "" : "";
            var employer = hit.TryGetProperty("employer", out var e) && e.TryGetProperty("name", out var n)
                ? n.GetString() ?? "" : "";
            var link = hit.TryGetProperty("webpage_url", out var w) ? w.GetString() ?? "" : "";
            if (headline.Length > 0 && link.Length > 0)
                result.Add((headline.Replace("[", "(").Replace("]", ")"), employer, link));
        }
        Console.WriteLine($"Jobs '{query}': {result.Count} hits");
        return result;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Jobs failed for '{query}': {e.Message}");
        return new();
    }
}

Dictionary<string, string> LoadSeen()
{
    try
    {
        if (!File.Exists(SeenPath)) return new();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SeenPath)) ?? new();
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Could not read {SeenPath}: {e.Message}");
        return new();
    }
}

void SaveSeen(Dictionary<string, string> all)
{
    // Forget entries older than 120 days so the file does not grow forever
    var cutoff = DateTime.UtcNow.AddDays(-120).ToString("yyyy-MM-dd");
    var pruned = all
        .Where(kv => string.CompareOrdinal(kv.Value, cutoff) >= 0)
        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
        .ToDictionary(kv => kv.Key, kv => kv.Value);

    Directory.CreateDirectory("digests");
    File.WriteAllText(SeenPath, JsonSerializer.Serialize(pruned, new JsonSerializerOptions { WriteIndented = true }));
}

IEnumerable<string> SplitForDiscord(string text, int max)
{
    var current = new StringBuilder();
    foreach (var line in text.Split('\n'))
    {
        var l = line.TrimEnd('\r');
        if (l.Length > max) l = l[..max];

        if (current.Length > 0 && current.Length + l.Length + 1 > max)
        {
            yield return current.ToString().TrimEnd();
            current.Clear();
        }
        current.Append(l).Append('\n');
    }
    if (current.Length > 0) yield return current.ToString().TrimEnd();
}