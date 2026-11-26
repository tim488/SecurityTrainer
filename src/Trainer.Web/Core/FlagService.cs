using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Trainer.Web.Core;

/// <summary>
/// Generates one random flag per lab per app start (<c>FLAG{slug-&lt;hex&gt;}</c>)
/// and checks submissions in constant time (SPEC §5.4).
/// </summary>
public sealed class FlagService
{
    private readonly ConcurrentDictionary<string, string> _flags = new(StringComparer.OrdinalIgnoreCase);

    public string GetFlag(string slug) =>
        _flags.GetOrAdd(slug, s => $"FLAG{{{s}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12))}}}");

    public bool Check(string slug, string? submitted)
    {
        var expected = Encoding.UTF8.GetBytes(GetFlag(slug));
        var actual = Encoding.UTF8.GetBytes((submitted ?? "").Trim());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
