using Xunit;

public class DiscordEmbedTests
{
    static Section Make(string title, int lines) =>
        new(title, string.Join("\n", Enumerable.Range(0, lines).Select(i => $"* [Headline {i}](https://example.com/{i})")), 0);

    [Fact]
    public void Embeds_GivesOneEmbedPerShortSection()
    {
        var embeds = Discord.Embeds([Make("A", 3), Make("B", 3)]).ToList();

        Assert.Equal(new[] { "A", "B" }, embeds.Select(e => e.Title));
    }

    [Fact]
    public void Embeds_ContinuesALongSectionInAnotherEmbed()
    {
        var embeds = Discord.Embeds([Make("Long", 300)]).ToList();

        Assert.True(embeds.Count > 1);
        Assert.Equal("Long", embeds[0].Title);
        Assert.All(embeds.Skip(1), e => Assert.Equal("Long (continued)", e.Title));
        Assert.All(embeds, e => Assert.True(e.Description.Length <= 4096));
    }

    [Fact]
    public void Pack_StaysUnderDiscordsPerMessageLimits()
    {
        var sections = Enumerable.Range(0, 30).Select(i => Make($"Section {i}", 1 + i * 5)).ToList();

        var messages = Discord.Pack(Discord.Embeds(sections)).ToList();

        Assert.All(messages, m =>
        {
            Assert.InRange(m.Count, 1, 10);
            Assert.True(m.Sum(e => e.Title.Length + e.Description.Length) <= 6000);
        });
        Assert.Equal(Discord.Embeds(sections).Count(), messages.Sum(m => m.Count));
    }
}
