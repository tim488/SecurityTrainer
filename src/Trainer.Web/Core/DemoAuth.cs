using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Trainer.Web.Core;

public sealed record DemoUser(string Mission, string Name);

/// <summary>
/// Demo authentication: each mission seeds users with random opaque
/// tokens generated at startup; tokens are looked up to find the user.
/// Players act as <see cref="Player"/>.
/// </summary>
public sealed class DemoAuth
{
    public const string Player = "wesley";

    private readonly ConcurrentDictionary<(string Mission, string User), string> _tokens = new();
    private readonly ConcurrentDictionary<string, DemoUser> _users = new(StringComparer.Ordinal);

    /// <summary>Returns the token for a mission user, creating it on first use.</summary>
    public string GetToken(string mission, string user) =>
        _tokens.GetOrAdd((mission.ToLowerInvariant(), user.ToLowerInvariant()), key =>
        {
            var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
            _users[token] = new DemoUser(key.Mission, key.User);
            return token;
        });

    public DemoUser? FindUser(string mission, string? token)
    {
        if (string.IsNullOrEmpty(token) || !_users.TryGetValue(token, out var user))
            return null;
        return string.Equals(user.Mission, mission, StringComparison.OrdinalIgnoreCase) ? user : null;
    }

    /// <summary>Reads <c>Authorization: Bearer &lt;token&gt;</c> and resolves the user for a mission.</summary>
    public DemoUser? FindUser(string mission, HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? FindUser(mission, header[prefix.Length..].Trim())
            : null;
    }
}
