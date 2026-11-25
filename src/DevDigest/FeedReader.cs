using System.Net;
using System.Text.Json;
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
            if (feed.Url.EndsWith(".json")) return await ReadJsonAsync(stream, feed, cts.Token);

            using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });

            return SyndicationFeed.Load(xml).Items
                .OrderByDescending(i => i.PublishDate == default ? i.LastUpdatedTime : i.PublishDate)
                .Select(i => new Item(
                    feed.Name,
                    (i.Title?.Text ?? "(untitled)").Trim(),
                    i.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    Snippet(i)))
                .Where(i => i.Link.Length > 0 && Matches(feed, i))
                .Take(40) // after the keyword filter, so a matching item further down is not cut off
                .ToList();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Feed failed {feed.Url}: {e.Message}");
            return [];
        }
    }

    /// <summary>
    /// A JSON news list shaped like vackelse.nu's data/results.json:
    /// { "items": [ { "title", "url", "summary" } ] }, newest first.
    /// </summary>
    static async Task<List<Item>> ReadJsonAsync(Stream stream, Feed feed, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(i => new Item(feed.Name, Str(i, "title").Trim(), Str(i, "url"), Str(i, "summary")))
            .Where(i => i.Title.Length > 0 && i.Link.Length > 0 && Matches(feed, i))
            .Take(40)
            .ToList();
    }

    static string Str(JsonElement e, string key) =>
        e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

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