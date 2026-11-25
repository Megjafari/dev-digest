record Item(string Source, string Title, string Link, string Snippet);

record Feed(string Section, string Name, string Url, string[]? Keywords = null);

/// <summary>One block of the digest: a heading and its markdown body. Becomes one Discord embed.</summary>
record Section(string Title, string Body, int Color);
