static class Config
{
    public const int PerFeed = 3;
    public const int MaxJobs = 20;
    public const int MaxInternships = 10;

    // Job ads that ask for more years of experience than this are skipped
    public const int MaxYearsExperience = 3;

    const string Christian = "Christian news";
    const string War = "War and conflicts";
    const string Backend = "Backend and .NET";
    const string DevOps = "DevOps and cloud";
    const string Security = "Security";
    const string Ai = "AI worldwide";
    const string Iran = "Iran";
    const string Sweden = "Sweden";

    static readonly string[] Conflict =
    [
        "iran", "tehran", "israel", "gaza", "palestin", "hamas", "hezbollah",
        "west bank", "lebanon", "houthi", "idf", "netanyahu", "khamenei",
    ];

    // The Persian outlets cover the whole region, so keep only items that mention Iran
    static readonly string[] IranWords = ["ایران", "تهران", "iran"];

    public static readonly Feed[] Feeds =
    [
        // First in the list, so the section comes right after the verse of the day
        new(Christian, "Christianity Today", "https://www.christianitytoday.com/feed/"),
        new(Christian, "The Christian Post", "https://www.christianpost.com/rss"),
        new(Christian, "Dagen", "https://www.dagen.se/rss"),
        new(Christian, "Mohabat News", "https://www.mohabatnews.com/index.php/feed"),
        new(Christian, "International Christian Concern", "https://www.persecution.org/feed/"),
        // No RSS, so read the JSON the site itself loads. The name is a markdown link to the site
        new(Christian, "[Väckelse.nu](https://vackelse.nu/#top)", "https://vackelse.nu/data/results.json"),

        // Mixed perspectives on purpose, filtered by keywords
        new(War, "Al Jazeera", "https://www.aljazeera.com/xml/rss/all.xml", Conflict),
        new(War, "BBC Middle East", "https://feeds.bbci.co.uk/news/world/middle_east/rss.xml", Conflict),
        new(War, "The Guardian", "https://www.theguardian.com/world/middleeast/rss", Conflict),
        
        // Persian and English, independent and state-linked outlets side by side
        new(Iran, "BBC Persian", "https://feeds.bbci.co.uk/persian/rss.xml", IranWords),
        new(Iran, "Independent Persian", "https://www.independentpersian.com/rss.xml", IranWords),
        new(Iran, "Tehran Times", "https://www.tehrantimes.com/rss"),
        new(Iran, "Press TV", "https://www.presstv.ir/rss.xml"),

        new(Sweden, "SVT Nyheter", "https://www.svt.se/nyheter/rss.xml"),
        new(Sweden, "Dagens Nyheter", "https://www.dn.se/rss/"),
        new(Sweden, "The Local Sweden", "https://www.thelocal.se/feed"),

        new(Backend, "Hacker News", "https://hnrss.org/frontpage"),
        new(Backend, ".NET Blog", "https://devblogs.microsoft.com/dotnet/feed/"),
        new(Backend, "Andrew Lock", "https://andrewlock.net/rss.xml"),
        new(Backend, "GitHub Blog", "https://github.blog/feed/"),
        new(Backend, "GitHub Changelog", "https://github.blog/changelog/feed/"),
        new(Backend, "PostgreSQL", "https://www.postgresql.org/news.rss"),

        new(DevOps, "CNCF", "https://www.cncf.io/feed/"),
        new(DevOps, "Kubernetes", "https://kubernetes.io/feed.xml"),
        new(DevOps, "Docker Blog", "https://www.docker.com/blog/feed/"),
        new(DevOps, "Argo Project", "https://blog.argoproj.io/feed"),
        new(DevOps, "HashiCorp", "https://www.hashicorp.com/blog/feed.xml"),
        new(DevOps, "Azure Blog", "https://azure.microsoft.com/en-us/blog/feed/"),
        new(DevOps, "Cloudflare", "https://blog.cloudflare.com/rss/"),
        new(DevOps, "The New Stack", "https://thenewstack.io/feed/"),
        new(DevOps, "dev.to devops", "https://dev.to/feed/tag/devops"),

        new(Security, "The Hacker News", "https://feeds.feedburner.com/TheHackersNews"),
        new(Security, "Krebs on Security", "https://krebsonsecurity.com/feed/"),
        new(Security, "GitHub Security", "https://github.blog/tag/security/feed/"),

        new(Ai, "Hacker News AI", "https://hnrss.org/newest?q=LLM&points=100"),
        new(Ai, "Simon Willison", "https://simonwillison.net/atom/everything/"),
        new(Ai, "The Verge AI", "https://www.theverge.com/rss/ai-artificial-intelligence/index.xml"),
        new(Ai, "MIT Technology Review", "https://www.technologyreview.com/topic/artificial-intelligence/feed"),
    ];

    // Swedish on purpose: the ads on JobTech are written in Swedish
    // All job searches are limited to Göteborg and the IT occupation field, see JobSearch
    public static readonly string[] JobQueries =
    [
        "junior utvecklare",
        ".NET",
        "C#",
        "backend",
        "devops",
        "cloud engineer",
        "platform engineer",
        "site reliability",
        "ci/cd",
        "kubernetes",
        "azure",
    ];

    // Few LIA places are posted on Platsbanken, so these searches are broad. Only ads with
    // LIA, praktik, trainee or internship in the headline are kept (JobSearch.IsInternship).
    public static readonly string[] InternshipQueries = ["LIA", "praktik", "praktikant", "internship", "trainee"];

    // Job ads whose headline has one of these words are skipped, to keep the list to junior and mid level roles
    public static readonly string[] SeniorWords =
    [
        "senior", "sr", "lead", "principal", "staff", "architect", "arkitekt",
        "head of", "chef", "manager", "erfaren", "erfarna", "expert",
    ];

    // Discord embed colors, the stripe on the left of each section
    public const int VerseColor = 0xC9A227;
    public const int JobsColor = 0x2ECC71;
    public const int InternshipColor = 0xF1C40F;
    public const int DefaultColor = 0x5865F2;

    public static readonly Dictionary<string, int> Colors = new()
    {
        [Christian] = 0xE8DAEF,
        [War] = 0xE74C3C,
        [Iran] = 0x16A085,
        [Sweden] = 0x3498DB,
        [Backend] = 0x9B59B6,
        [DevOps] = 0x1F8FD6,
        [Security] = 0x7F8C8D,
        [Ai] = 0xE67E22,
    };
}
