namespace Trainer.Web.Core.Simulation;

/// <summary>
/// An in-memory, Unix-style filesystem (SPEC §5.5) so labs never touch the host.
/// <see cref="Combine"/> and <see cref="GetFullPath"/> behave like their
/// <see cref="Path"/> counterparts on Linux, so vulnerable code behaves like the real thing.
/// </summary>
public sealed class VirtualFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public void WriteAllText(string path, string content) => _files[GetFullPath(path)] = content;

    public bool Exists(string path) => _files.ContainsKey(GetFullPath(path));

    public string? ReadAllText(string path) => _files.GetValueOrDefault(GetFullPath(path));

    /// <summary>Files directly inside <paramref name="directory"/>, as full paths.</summary>
    public IReadOnlyList<string> List(string directory)
    {
        var prefix = GetFullPath(directory).TrimEnd('/') + "/";
        return _files.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal) && !k[prefix.Length..].Contains('/'))
            .Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>Like <see cref="Path.Combine(string, string)"/>: a rooted second path discards the first.</summary>
    public static string Combine(string first, string second)
    {
        if (second.StartsWith('/')) return second;
        if (first.Length == 0) return second;
        return first.EndsWith('/') ? first + second : first + "/" + second;
    }

    /// <summary>Like <see cref="Path.GetFullPath(string)"/>: resolves <c>.</c> and <c>..</c>; <c>..</c> above the root stays at the root.</summary>
    public static string GetFullPath(string path)
    {
        var parts = new List<string>();
        foreach (var segment in path.Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == "..")
            {
                if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
                continue;
            }
            parts.Add(segment);
        }
        return "/" + string.Join('/', parts);
    }
}
