static class Config
{
    public const int PerFeed = 3;
    public const int MaxJobs = 15;

    const string War = "War and conflicts";
    const string Dev = "DevOps and software development";
    const string Ai = "AI worldwide";

    static readonly string[] Conflict =
    [
        "iran", "tehran", "israel", "gaza", "palestin", "hamas", "hezbollah",
        "west bank", "lebanon", "houthi", "idf", "netanyahu", "khamenei",
    ];

    public static readonly Feed[] Feeds =
    [
        // Mixed perspectives on purpose, filtered by keywords
        new(War, "Al Jazeera", "https://www.aljazeera.com/xml/rss/all.xml", Conflict),
        new(War, "BBC Middle East", "https://feeds.bbci.co.uk/news/world/middle_east/rss.xml", Conflict),
        new(War, "The Guardian", "https://www.theguardian.com/world/middleeast/rss", Conflict),
        
        new(Dev, "Hacker News", "https://hnrss.org/frontpage"),
        new(Dev, "dev.to devops", "https://dev.to/feed/tag/devops"),
        new(Dev, "CNCF", "https://www.cncf.io/feed/"),
        new(Dev, "Docker Blog", "https://www.docker.com/blog/feed/"),
        new(Dev, "Kubernetes", "https://kubernetes.io/feed.xml"),
        new(Dev, ".NET Blog", "https://devblogs.microsoft.com/dotnet/feed/"),
        new(Dev, "Azure Blog", "https://azure.microsoft.com/en-us/blog/feed/"),
        new(Dev, "GitHub Changelog", "https://github.blog/changelog/feed/"),
        new(Dev, "PostgreSQL", "https://www.postgresql.org/news.rss"),

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

    public static readonly Dictionary<string, string> Focus = new()
    {
        [War] = "This is conflict news. Stay strictly neutral, attribute every claim to its source (for example 'according to Al Jazeera'), and point out where sources differ.",
        [Dev] = "The reader is a backend and cloud developer working with .NET, Azure, Docker, Kubernetes and PostgreSQL. Lead with what matters most for that work.",
        [Ai] = "Lead with the most significant developments and say briefly why they matter.",
    };

    public const string JobsFocus =
        "These are new job ads (the source in brackets is the employer). Say what kinds of roles and employers show up, and mention any that stand out for a junior .NET, cloud or DevOps developer in Gothenburg.";
}