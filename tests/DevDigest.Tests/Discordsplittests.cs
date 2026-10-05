using Xunit;

public class DiscordSplitTests
{
    static string[] Lines(int count) =>
        Enumerable.Range(0, count).Select(i => $"* [Title {i}](https://example.com/{i}) (Source)").ToArray();

    [Fact]
    public void Split_ReturnsOneChunk_WhenTextIsShort()
    {
        var chunks = Discord.Split("line one\nline two").ToList();

        Assert.Single(chunks);
    }

    [Fact]
    public void Split_NeverExceedsDiscordsLimit()
    {
        var chunks = Discord.Split(string.Join("\n", Lines(200))).ToList();

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= 2000));
    }

    [Fact]
    public void Split_KeepsEveryLineIntact_SoNoLinkIsCutInHalf()
    {
        var lines = Lines(200);

        var chunks = Discord.Split(string.Join("\n", lines)).ToList();

        Assert.Equal(lines, string.Join("\n", chunks).Split('\n'));
    }

    [Fact]
    public void Split_TruncatesASingleLineThatIsLongerThanTheLimit()
    {
        var chunks = Discord.Split(new string('x', 5000)).ToList();

        Assert.Single(chunks);
        Assert.Equal(1900, chunks[0].Length);
    }
}