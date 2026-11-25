using Xunit;

public class JobFilterTests
{
    static Item Job(string title) => new("Employer", title, $"https://example.com/{title}", "");

    [Theory]
    [InlineData("Senior DevOps ingenjör till Syntronic")]
    [InlineData("Tech Lead .NET")]
    [InlineData("Erfarna systemutvecklare C# och .NET")]
    [InlineData("Sr. Backend Developer")]
    [InlineData("Cloud Architect")]
    public void IsSenior_CatchesSeniorHeadlines(string title) =>
        Assert.True(JobSearch.IsSenior(Job(title), Config.SeniorWords));

    [Theory]
    [InlineData("Junior .NET utvecklare")]
    [InlineData("DevOps Engineer")]
    [InlineData("Platform Engineer  Cloud Security")]
    [InlineData("Teknisk projektledare")] // "ledare" is not "lead"
    [InlineData("Leading fintech söker backendutvecklare")] // "leading" is not "lead"
    public void IsSenior_KeepsOtherHeadlines(string title) =>
        Assert.False(JobSearch.IsSenior(Job(title), Config.SeniorWords));

    [Fact]
    public void Interleave_TakesOneFromEachSearchInTurn()
    {
        List<List<Item>> lists =
        [
            [Job("a1"), Job("a2"), Job("a3")],
            [Job("b1")],
            [Job("c1"), Job("c2")],
        ];

        var titles = JobSearch.Interleave(lists).Select(j => j.Title);

        Assert.Equal(new[] { "a1", "b1", "c1", "a2", "c2", "a3" }, titles);
    }

    [Theory]
    [InlineData("LIA-praktik inom agil utveckling")]
    [InlineData("LIA-plats .NET")]
    [InlineData("Praktikplats systemutvecklare")]
    [InlineData("Software Engineering Internship")]
    [InlineData("Trainee Cloud")]
    public void IsInternship_CatchesLiaAndPraktik(string title) =>
        Assert.True(JobSearch.IsInternship(Job(title)));

    [Theory]
    [InlineData("Liability Analyst")]
    [InlineData("Internal IT Support")]
    [InlineData("International Backend Developer")]
    public void IsInternship_IgnoresLookalikeWords(string title) =>
        Assert.False(JobSearch.IsInternship(Job(title)));

    [Theory]
    [InlineData("Du har minst 5 års erfarenhet av C#.", 5)]
    [InlineData("You have 3+ years of professional experience with Azure.", 3)]
    [InlineData("Vi söker dig med 2-4 års erfarenhet av backend.", 2)]
    [InlineData("1 års erfarenhet av Docker och 6 års erfarenhet av .NET", 6)]
    [InlineData("Vi har funnits i 25 år och växer.", 0)]
    [InlineData("Ingen tidigare erfarenhet krävs.", 0)]
    public void YearsRequired_ReadsTheExperienceRequirement(string text, int expected) =>
        Assert.Equal(expected, JobSearch.YearsRequired(new Item("Employer", "Developer", "https://example.com", text)));
}
