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
        if (!File.Exists(FilePath))
        {
            Console.WriteLine($"No {FilePath} yet, starting with an empty seen list");
            return new([]);
        }

        try { return new(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? []); }
        catch (Exception e) when (e is JsonException or IOException)
        {
            // Starting over can resend recent links, but failing every run would stop the digest completely
            Console.Error.WriteLine($"WARNING: could not read {FilePath}, starting with an empty seen list: {e.Message}");
            return new([]);
        }
    }

    /// <summary>Returns up to <paramref name="max"/> unseen items and marks them as seen.</summary>
    public List<Item> TakeNew(IEnumerable<Item> items, int max)
    {
        var result = new List<Item>();
        foreach (var item in items)
        {
            if (result.Count >= max) break;
            if (_seen.TryAdd(item.Link, _today)) result.Add(item);
        }
        return result;
    }

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
