using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Trainer.Web.Core;

public sealed record DemoUser(string Lab, string Name);

/// <summary>
/// Demo authentication (SPEC §5.3): each lab seeds users with random opaque
/// tokens generated at startup; tokens are looked up to find the user.
/// Players act as <see cref="Player"/>.
/// </summary>
public sealed class DemoAuth
{
    public const string Player = "wesley";

    private readonly ConcurrentDictionary<(string Lab, string User), string> _tokens = new();
    private readonly ConcurrentDictionary<string, DemoUser> _users = new(StringComparer.Ordinal);

    /// <summary>Returns the token for a lab user, creating it on first use.</summary>
    public string GetToken(string lab, string user) =>
        _tokens.GetOrAdd((lab.ToLowerInvariant(), user.ToLowerInvariant()), key =>
        {
            var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
            _users[token] = new DemoUser(key.Lab, key.User);
            return token;
        });

    public DemoUser? FindUser(string lab, string? token)
    {
        if (string.IsNullOrEmpty(token) || !_users.TryGetValue(token, out var user))
            return null;
        return string.Equals(user.Lab, lab, StringComparison.OrdinalIgnoreCase) ? user : null;
    }

    /// <summary>Reads <c>Authorization: Bearer &lt;token&gt;</c> and resolves the user for a lab.</summary>
    public DemoUser? FindUser(string lab, HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? FindUser(lab, header[prefix.Length..].Trim())
            : null;
    }
}
