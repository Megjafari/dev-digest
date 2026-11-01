// DRY_RUN=1: write to digests-preview/ and leave the "seen" list untouched (safe for local testing)
var dryRun = Environment.GetEnvironmentVariable("DRY_RUN") == "1";
var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("dev-digest/1.0 (+https://github.com/Megjafari)");

var seen = SeenStore.Load();
var verse = await VerseOfTheDay.GetAsync(http);

var news = "";
foreach (var group in Config.Feeds.GroupBy(f => f.Section))
{
    var items = new List<Item>();
    foreach (var feed in group)
        items.AddRange(seen.TakeNew(await FeedReader.ReadAsync(http, feed), Config.PerFeed));

    news += Digest.Render($"### {group.Key}", items);
}

var jobs = new List<Item>();
foreach (var query in Config.JobQueries)
{
    if (jobs.Count >= Config.MaxJobs) break;
    jobs.AddRange(seen.TakeNew(await JobSearch.SearchAsync(http, query), Config.MaxJobs - jobs.Count));
}
var jobsMd = Digest.Render("## New job ads", jobs, "No new job ads today.", perSource: false);

var markdown = $"# Digest {today}\n\n{verse}## News\n\n{(news.Length > 0 ? news : "No new news today.\n\n")}{jobsMd}";

var outDir = dryRun ? "digests-preview" : "digests";
Directory.CreateDirectory(outDir);
var path = Path.Combine(outDir, $"{today}.md");
File.WriteAllText(path, markdown);
Console.WriteLine($"Wrote {path}");

if (!dryRun) seen.Save();

await Discord.SendAsync(http, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK"), markdown);