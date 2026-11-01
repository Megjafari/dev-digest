using System.Text;

static class Digest
{
    /// <summary>
    /// One section. News is grouped per source, so each source label sits directly above its own links.
    /// Set <paramref name="perSource"/> to false for one list over everything (used for jobs).
    /// </summary>
    public static string Render(string heading, List<Item> items, string? emptyText = null, bool perSource = true)
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

            foreach (var i in group)
                sb.Append($"* [{i.Title.Replace('[', '(').Replace(']', ')')}]({i.Link}){(perSource ? "" : $" ({i.Source})")}\n");
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
