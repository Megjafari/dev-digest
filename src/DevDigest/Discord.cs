using System.Net;
using System.Net.Http.Json;
using System.Text;

static class Discord
{
    // Discord's limits are 4096 characters per embed description, 10 embeds and 6000 embed characters per message
    const int MaxDescription = 4000;
    const int MaxEmbedsPerMessage = 10;
    const int MaxCharsPerMessage = 5800;

    internal record Embed(string Title, string Description, int Color);

    /// <summary>Posts the digest as embeds, one per section, packed into as few messages as Discord allows.</summary>
    public static async Task SendAsync(HttpClient http, string? webhook, string title, IEnumerable<Section> sections)
    {
        if (string.IsNullOrWhiteSpace(webhook))
        {
            Console.WriteLine("DISCORD_WEBHOOK not set, skipping delivery");
            return;
        }

        // Never ping anyone, even if a headline contains @everyone
        var noMentions = new { parse = Array.Empty<string>() };

        var first = true;
        foreach (var batch in Pack(Embeds(sections)))
        {
            object payload = first
                ? new { content = $"**{title}**", embeds = batch, allowed_mentions = noMentions }
                : new { embeds = batch, allowed_mentions = noMentions };
            await PostAsync(http, webhook, payload);
            first = false;
            await Task.Delay(1000); // stay under the webhook rate limit
        }
    }

    /// <summary>
    /// Throws if Discord does not accept the message, so the workflow fails and the seen list
    /// is not saved. The same links are then sent again on the next run instead of being lost.
    /// </summary>
    static async Task PostAsync(HttpClient http, string webhook, object payload)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var resp = await http.PostAsJsonAsync(webhook, payload);
            Console.WriteLine($"Discord: {(int)resp.StatusCode}");
            if (resp.IsSuccessStatusCode) return;

            if (resp.StatusCode == HttpStatusCode.TooManyRequests && attempt < 3)
            {
                await Task.Delay(resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2));
                continue;
            }
            throw new HttpRequestException($"Discord {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        }
    }

    /// <summary>One embed per section. A section longer than one embed continues in the next.</summary>
    internal static IEnumerable<Embed> Embeds(IEnumerable<Section> sections) =>
        sections.SelectMany(s => Split(s.Body, MaxDescription)
            .Select((chunk, i) => new Embed(i == 0 ? s.Title : $"{s.Title} (continued)", chunk, s.Color)));

    /// <summary>Groups embeds into messages that stay under Discord's per-message limits.</summary>
    internal static IEnumerable<List<Embed>> Pack(IEnumerable<Embed> embeds)
    {
        var batch = new List<Embed>();
        var size = 0;
        foreach (var embed in embeds)
        {
            var length = embed.Title.Length + embed.Description.Length;
            if (batch.Count > 0 && (batch.Count == MaxEmbedsPerMessage || size + length > MaxCharsPerMessage))
            {
                yield return batch;
                batch = [];
                size = 0;
            }
            batch.Add(embed);
            size += length;
        }
        if (batch.Count > 0) yield return batch;
    }

    /// <summary>Splits on line breaks so no link is cut in half.</summary>
    internal static IEnumerable<string> Split(string text, int maxLength)
    {
        var current = new StringBuilder();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length > maxLength) line = line[..maxLength];

            if (current.Length > 0 && current.Length + line.Length + 1 > maxLength)
            {
                yield return current.ToString().TrimEnd();
                current.Clear();
            }
            current.Append(line).Append('\n');
        }
        if (current.Length > 0) yield return current.ToString().TrimEnd();
    }
}
