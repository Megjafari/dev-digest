static class Config
{
    public const int PerFeed = 3;
    public const int MaxJobs = 15;

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
    public static readonly string[] JobQueries =
    [
        "devops göteborg",
        ".NET utvecklare göteborg",
        "molnutvecklare azure",
        "junior systemutvecklare",
    ];

    // Discord embed colors, the stripe on the left of each section
    public const int VerseColor = 0xC9A227;
    public const int JobsColor = 0x2ECC71;
    public const int DefaultColor = 0x5865F2;

    public static readonly Dictionary<string, int> Colors = new()
    {
        [War] = 0xE74C3C,
        [Iran] = 0x16A085,
        [Sweden] = 0x3498DB,
        [Backend] = 0x9B59B6,
        [DevOps] = 0x1F8FD6,
        [Security] = 0x7F8C8D,
        [Ai] = 0xE67E22,
    };
}
