using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;

static class FeedReader
{
    public static async Task<List<Item>> ReadAsync(HttpClient http, Feed feed)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await using var stream = await http.GetStreamAsync(feed.Url, cts.Token);
            using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });

            return SyndicationFeed.Load(xml).Items
                .OrderByDescending(i => i.PublishDate == default ? i.LastUpdatedTime : i.PublishDate)
                .Take(40)
                .Select(i => new Item(
                    feed.Name,
                    (i.Title?.Text ?? "(untitled)").Trim(),
                    i.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Snippet(i)))
                .Where(i => i.Link.Length > 0 && Matches(feed, i))
                .ToList();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Feed failed {feed.Url}: {e.Message}");
            return [];
        }
    }

    internal static bool Matches(Feed feed, Item item) =>
        feed.Keywords is null ||
        feed.Keywords.Any(k => $"{item.Title} {item.Snippet}".Contains(k, StringComparison.OrdinalIgnoreCase));

    static string Snippet(SyndicationItem item)
    {
        var html = item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text ?? "";
        var text = Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<.*?>", " ")), @"\s+", " ").Trim();
        return text.Length > 250 ? text[..250] : text;
    }
}