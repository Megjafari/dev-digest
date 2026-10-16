using System.Text.Json;

static class VerseOfTheDay
{
    // Used if the API cannot be reached, so the digest still has a verse link at the top
    const string Fallback = "## Verse of the day\n\n[Today's verse on BibleGateway](https://www.biblegateway.com/)\n\n";

    /// <summary>Today's verse from OurManna: the verse text, then the reference as a link to BibleGateway.</summary>
    public static async Task<string> GetAsync(HttpClient http)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var json = await http.GetStringAsync("https://beta.ourmanna.com/api/v1/get?format=json&order=daily", cts.Token);
            using var doc = JsonDocument.Parse(json);

            var details = doc.RootElement.GetProperty("verse").GetProperty("details");
            var reference = details.GetProperty("reference").GetString() ?? "";
            var version = details.TryGetProperty("version", out var v) ? v.GetString() ?? "NIV" : "NIV";
            var text = details.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (reference.Length == 0) throw new InvalidOperationException("no reference in response");

            var link = $"https://www.biblegateway.com/passage/?search={Uri.EscapeDataString(reference)}&version={version}";
            var source = $"[{reference}]({link}) ({version}, via OurManna)";

            return text.Length == 0
                ? $"## Verse of the day\n\n{source}\n\n"
                : $"## Verse of the day\n\n> {text}\n>\n> {source}\n\n";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Verse failed, using fallback link: {e.Message}");
            return Fallback;
        }
    }
}