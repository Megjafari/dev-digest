// DRY_RUN=1: write to digests-preview/ and leave the "seen" list untouched (safe for local testing)
var dryRun = Environment.GetEnvironmentVariable("DRY_RUN") == "1";
var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

// The workflow has several schedules as backups for GitHub's delays. Only the first one each day sends.
const string LastSentPath = "digests/last-sent.txt";
if (Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") == "schedule" &&
    File.Exists(LastSentPath) && File.ReadAllText(LastSentPath).Trim() == today)
{
    Console.WriteLine($"Digest for {today} already sent, nothing to do");
    return;
}

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("dev-digest/1.0 (+https://github.com/Megjafari)");

var seen = SeenStore.Load();
var verse = await VerseOfTheDay.GetAsync(http);

var sections = new List<Section> { new("Verse of the day", verse, Config.VerseColor) };

foreach (var group in Config.Feeds.GroupBy(f => f.Section))
{
    var items = new List<Item>();
    foreach (var feed in group)
        items.AddRange(seen.TakeNew(await FeedReader.ReadAsync(http, feed), Config.PerFeed));

    if (items.Count > 0)
        sections.Add(new(group.Key, Digest.Render(items), Config.Colors.GetValueOrDefault(group.Key, Config.DefaultColor)));
}
if (sections.Count == 1) sections.Add(new("News", "No new news today.", Config.DefaultColor));

var jobs = new List<Item>();
foreach (var query in Config.JobQueries)
{
    if (jobs.Count >= Config.MaxJobs) break;
    jobs.AddRange(seen.TakeNew(await JobSearch.SearchAsync(http, query), Config.MaxJobs - jobs.Count));
}
sections.Add(new("New job ads", jobs.Count > 0 ? Digest.Render(jobs, perSource: false) : "No new job ads today.", Config.JobsColor));

var title = $"Digest {today}";
var markdown = Digest.ToMarkdown(title, sections);

var outDir = dryRun ? "digests-preview" : "digests";
Directory.CreateDirectory(outDir);
var path = Path.Combine(outDir, $"{today}.md");
File.WriteAllText(path, markdown);
Console.WriteLine($"Wrote {path}");

// Only remember the links once Discord has them, so a failed or skipped delivery sends them again next time
var delivered = await Discord.SendAsync(http, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK"), title, sections);
if (!dryRun && delivered)
{
    seen.Save();
    File.WriteAllText(LastSentPath, today);
}
else if (!dryRun) Console.WriteLine("Nothing delivered, seen list left unchanged");
