using System.Text;

static class Digest
{
    /// <summary>
    /// The body of one section. News is grouped per source, so each source label sits directly above its own links.
    /// Set <paramref name="perSource"/> to false for one list over everything (used for jobs).
    /// </summary>
    public static string Render(List<Item> items, bool perSource = true)
    {
        var blocks = perSource
            ? items.GroupBy(i => i.Source).Select(g => (Label: g.Key, Items: g.ToList())).ToList()
            : new List<(string Label, List<Item> Items)> { ("", items) };

        var sb = new StringBuilder();
        foreach (var (label, group) in blocks)
        {
            if (label.Length > 0) sb.Append($"**{label}**\n");

            foreach (var i in group)
                sb.Append($"* [{i.Title.Replace('[', '(').Replace(']', ')')}]({i.Link}){(perSource ? "" : $" ({i.Source})")}\n");
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>The whole digest as one markdown document, for the file written next to the Discord delivery.</summary>
    public static string ToMarkdown(string title, IEnumerable<Section> sections) =>
        $"# {title}\n\n" + string.Concat(sections.Select(s => $"## {s.Title}\n\n{s.Body}\n\n"));
}
