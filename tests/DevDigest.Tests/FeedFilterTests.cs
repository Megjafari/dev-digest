using Xunit;

public class FeedFilterTests
{
    static Item Make(string title, string snippet = "") => new("source", title, "https://example.com", snippet);

    [Fact]
    public void Matches_AcceptsEverything_WhenFeedHasNoKeywords()
    {
        var feed = new Feed("Section", "Name", "https://example.com/feed");

        Assert.True(FeedReader.Matches(feed, Make("Anything at all")));
    }

    [Fact]
    public void Matches_FindsKeywordInTheTitle_IgnoringCase()
    {
        var feed = new Feed("Section", "Name", "https://example.com/feed", ["iran"]);

        Assert.True(FeedReader.Matches(feed, Make("Talks on IRAN nuclear deal resume")));
    }

    [Fact]
    public void Matches_FindsKeywordInTheSnippet()
    {
        var feed = new Feed("Section", "Name", "https://example.com/feed", ["gaza"]);

        Assert.True(FeedReader.Matches(feed, Make("Ceasefire update", "Aid reaches northern Gaza")));
    }

    [Fact]
    public void Matches_RejectsItemsWithoutAnyKeyword()
    {
        var feed = new Feed("Section", "Name", "https://example.com/feed", ["iran", "gaza"]);

        Assert.False(FeedReader.Matches(feed, Make("New release of a database engine")));
    }
}