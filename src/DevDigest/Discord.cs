using System.Net.Http.Json;
using System.Text;

static class Discord
{
    const int MaxLength = 1900; // Discord's limit is 2000 characters per message

    public static async Task SendAsync(HttpClient http, string? webhook, string markdown)
    {
        if (string.IsNullOrWhiteSpace(webhook))
        {
            Console.WriteLine("DISCORD_WEBHOOK not set, skipping delivery");
            return;
        }

        foreach (var chunk in Split(markdown))
        {
            // flags = 4 turns off link previews
            var resp = await http.PostAsJsonAsync(webhook, new { content = chunk, flags = 4 });
            Console.WriteLine($"Discord: {(int)resp.StatusCode}");
            await Task.Delay(1000); // stay under the webhook rate limit
        }
    }

    /// <summary>Splits on line breaks so no link is cut in half.</summary>
    static IEnumerable<string> Split(string text)
    {
        var current = new StringBuilder();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length > MaxLength) line = line[..MaxLength];

            if (current.Length > 0 && current.Length + line.Length + 1 > MaxLength)
            {
                yield return current.ToString().TrimEnd();
                current.Clear();
            }
            current.Append(line).Append('\n');
        }
        if (current.Length > 0) yield return current.ToString().TrimEnd();
    }
}