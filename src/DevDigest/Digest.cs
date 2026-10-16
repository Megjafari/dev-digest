using System.Text;

static class Digest
{
    /// <summary>
    /// One section. News is summarized per source, so each summary sits directly above its own links.
    /// Set <paramref name="perSource"/> to false for one summary over the whole list (used for jobs).
    /// </summary>
    public static async Task<string> RenderAsync(ISummarizer summarizer, string heading, List<Item> items, string focus,
        string? emptyText = null, bool perSource = true)
    {
        if (items.Count == 0)
            return emptyText is null ? "" : $"{heading}\n\n{emptyText}\n\n";

        var blocks = perSource
            ? items.GroupBy(i => i.Source).Select(g => (Label: g.Key, Items: g.ToList())).ToList()
            : new List<(string Label, List<Item> Items)> { ("", items) };

        var sb = new StringBuilder($"{heading}\n\n");
        foreach (var (label, group) in blocks)
        {
            if (label.Length > 0) sb.Append($"**{label}**\n\n");

            var summary = await summarizer.SummarizeAsync(group, focus);
            if (summary is not null) sb.Append($"{summary}\n\n");

            if (!perSource) sb.Append("**Links**\n");
            foreach (var i in group)
                sb.Append($"* [{i.Title.Replace('[', '(').Replace(']', ')')}]({i.Link}){(perSource ? "" : $" ({i.Source})")}\n");
            sb.Append('\n');
        }
        return sb.ToString();
    }
}