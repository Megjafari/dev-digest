using Xunit;

public class DiscordSplitTests
{
    static string[] Lines(int count) =>
        Enumerable.Range(0, count).Select(i => $"* [Title {i}](https://example.com/{i}) (Source)").ToArray();

    [Fact]
    public void Split_ReturnsOneChunk_WhenTextIsShort()
    {
        var chunks = Discord.Split("line one\nline two", 4000).ToList();

        Assert.Single(chunks);
    }

    [Fact]
    public void Split_NeverExceedsTheLimit()
    {
        var chunks = Discord.Split(string.Join("\n", Lines(200)), 4000).ToList();

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= 4000));
    }

    [Fact]
    public void Split_KeepsEveryLineIntact_SoNoLinkIsCutInHalf()
    {
        var lines = Lines(200);

        var chunks = Discord.Split(string.Join("\n", lines), 4000).ToList();

        Assert.Equal(lines, string.Join("\n", chunks).Split('\n'));
    }

    [Fact]
    public void Split_TruncatesASingleLineThatIsLongerThanTheLimit()
    {
        var chunks = Discord.Split(new string('x', 5000), 4000).ToList();

        Assert.Single(chunks);
        Assert.Equal(4000, chunks[0].Length);
    }
}
