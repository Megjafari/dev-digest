using Xunit;

public class SeenStoreTests
{
    static Item Make(string link) => new("source", "title", link, "");

    [Fact]
    public void TakeNew_ReturnsEverything_WhenNothingHasBeenSeen()
    {
        var store = new SeenStore([]);

        var result = store.TakeNew([Make("a"), Make("b")], 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void TakeNew_SkipsLinksThatWereAlreadyTaken()
    {
        var store = new SeenStore([]);
        store.TakeNew([Make("a")], 10);

        var second = store.TakeNew([Make("a"), Make("b")], 10);

        Assert.Single(second);
        Assert.Equal("b", second[0].Link);
    }

    [Fact]
    public void TakeNew_RespectsMax_AndLeavesTheRestUnmarked()
    {
        var store = new SeenStore([]);

        var first = store.TakeNew([Make("a"), Make("b"), Make("c")], 2);
        var later = store.TakeNew([Make("c")], 10);

        Assert.Equal(new[] { "a", "b" }, first.Select(i => i.Link));
        Assert.Single(later); // "c" was not taken the first time, so it is still new
    }

    [Fact]
    public void TakeNew_DeduplicatesWithinTheSameBatch()
    {
        var store = new SeenStore([]);

        var result = store.TakeNew([Make("a"), Make("a")], 10);

        Assert.Single(result);
    }
}