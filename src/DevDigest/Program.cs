using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Net.Http.Json;

var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("dev-digest/1.0 (+https://github.com/Megjafari)");

var feeds = new (string Section, string Name, string Url)[]
{
    ("DevOps och tech", "Hacker News", "https://hnrss.org/frontpage"),
    ("DevOps och tech", "dev.to devops", "https://dev.to/feed/tag/devops"),
    ("DevOps och tech", "CNCF", "https://www.cncf.io/feed/"),
    ("Azure", "Azure Blog", "https://azure.microsoft.com/en-us/blog/feed/"),
    ("AI", "Hacker News AI", "https://hnrss.org/newest?q=AI+OR+LLM&points=100"),
    ("Varlden", "BBC World", "https://feeds.bbci.co.uk/news/world/rss.xml"),
};

var now = DateTime.UtcNow;
var sb = new StringBuilder();
sb.AppendLine($"# Digest {now:yyyy-MM-dd}");
sb.AppendLine();

foreach (var group in feeds.GroupBy(f => f.Section))
{
    sb.AppendLine($"## {group.Key}");
    sb.AppendLine();
    foreach (var feed in group)
    {
        var items = await ReadFeed(feed.Url);
        if (items.Count == 0) continue;
        sb.AppendLine($"### {feed.Name}");
        foreach (var (title, link) in items)
            sb.AppendLine($"* [{title}]({link})");
        sb.AppendLine();
    }
}

var jobs = await ReadJobs("devops OR .NET göteborg");
if (jobs.Count > 0)
{
    sb.AppendLine("## Jobb och LIA (Arbetsförmedlingen)");
    sb.AppendLine();
    foreach (var (headline, employer, url) in jobs)
        sb.AppendLine($"* [{headline}]({url}) ({employer})");
    sb.AppendLine();
}

var markdown = sb.ToString();

Directory.CreateDirectory("digests");
var path = Path.Combine("digests", $"{now:yyyy-MM-dd}.md");
await File.WriteAllTextAsync(path, markdown);
Console.WriteLine($"Wrote {path}");

var webhook = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK");
if (!string.IsNullOrWhiteSpace(webhook))
{
    var content = markdown.Length > 1900 ? markdown[..1900] + "\n..." : markdown;
    var resp = await http.PostAsJsonAsync(webhook, new { content });
    Console.WriteLine($"Discord: {(int)resp.StatusCode}");
}
else
{
    Console.WriteLine("DISCORD_WEBHOOK not set, skipping delivery");
}

async Task<List<(string Title, string Link)>> ReadFeed(string url)
{
    try
    {
        await using var stream = await http.GetStreamAsync(url);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var feed = SyndicationFeed.Load(reader);
        return feed.Items
            .OrderByDescending(i => i.PublishDate)
            .Take(5)
            .Select(i => ((i.Title?.Text ?? "(utan titel)").Trim().Replace("[", "(").Replace("]", ")"),
                          i.Links.FirstOrDefault()?.Uri.ToString() ?? ""))
            .ToList();
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
        var url = "https://jobsearch.api.jobtechdev.se/search?limit=8&sort=pubdate-desc&q=" + Uri.EscapeDataString(query);
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
        return result;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Jobs failed: {e.Message}");
        return new();
    }
}
