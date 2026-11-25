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

// LIA first, so a LIA ad found by a job search too lands in its own section
var internships = await FindJobsAsync(Config.InternshipQueries, JobSearch.IsInternship, Config.MaxInternships);
sections.Add(new("LIA and praktik", internships.Count > 0 ? Digest.Render(internships, perSource: false) : "No new LIA or praktik today.", Config.InternshipColor));

var jobs = await FindJobsAsync(Config.JobQueries,
    j => !JobSearch.IsInternship(j) && !JobSearch.IsSenior(j, Config.SeniorWords) && JobSearch.YearsRequired(j) <= Config.MaxYearsExperience,
    Config.MaxJobs);
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

async Task<List<Item>> FindJobsAsync(string[] queries, Func<Item, bool> keep, int max)
{
    var perQuery = new List<List<Item>>();
    foreach (var query in queries)
    {
        // Platsbanken first, then the other sites, so an ad on both keeps its Platsbanken link
        var found = await JobSearch.PlatsbankenAsync(http, query);
        found.AddRange(await JobSearch.LinksAsync(http, query));
        perQuery.Add(found.Where(keep).ToList());
    }

    // Every search gets a fair share of the limit. The same ad can be listed on several sites
    // with different links, so repeated headlines are skipped as well.
    return seen.TakeNew(JobSearch.Interleave(perQuery).DistinctBy(j => j.Title), max);
}
