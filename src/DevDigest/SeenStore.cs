using System.Text.Json;

/// <summary>Remembers every link already sent, so nothing is ever delivered twice.</summary>
class SeenStore
{
    const string FilePath = "digests/seen.json";
    const int KeepDays = 120;

    readonly Dictionary<string, string> _seen;
    readonly string _today = DateTime.UtcNow.ToString("yyyy-MM-dd");

    internal SeenStore(Dictionary<string, string> seen) => _seen = seen;

    public static SeenStore Load()
    {
        try { return new(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? []); }
        catch { return new([]); }
    }

    /// <summary>Returns up to <paramref name="max"/> unseen items and marks them as seen.</summary>
    public List<Item> TakeNew(IEnumerable<Item> items, int max) =>
        items.Where(i => _seen.TryAdd(i.Link, _today)).Take(max).ToList();

    public void Save()
    {
        var cutoff = DateTime.UtcNow.AddDays(-KeepDays).ToString("yyyy-MM-dd");
        var kept = _seen
            .Where(kv => string.CompareOrdinal(kv.Value, cutoff) >= 0)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Directory.CreateDirectory("digests");
        File.WriteAllText(FilePath, JsonSerializer.Serialize(kept, new JsonSerializerOptions { WriteIndented = true }));
    }
}