static class Digest
{
    /// <summary>One section: heading, AI summary, then the links. Empty sections are skipped unless <paramref name="emptyText"/> is given.</summary>
    public static async Task<string> RenderAsync(Summarizer summarizer, string heading, List<Item> items, string focus, string? emptyText = null)
    {
        if (items.Count == 0)
            return emptyText is null ? "" : $"{heading}\n\n{emptyText}\n\n";

        var summary = await summarizer.SummarizeAsync(items, focus);
        var links = items.Select(i => $"* [{i.Title.Replace('[', '(').Replace(']', ')')}]({i.Link}) ({i.Source})");

        return $"{heading}\n\n{(summary is null ? "" : summary + "\n\n")}**Links**\n{string.Join("\n", links)}\n\n";
    }
}