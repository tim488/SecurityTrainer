using System.Text;

namespace Trainer.Web.Core;

/// <summary>Base64url without padding, as used by JWTs (RFC 7515).</summary>
public static class Base64Url
{
    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Encode(string text) => Encode(Encoding.UTF8.GetBytes(text));

    public static bool TryDecode(string value, out byte[] bytes)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        try
        {
            bytes = Convert.FromBase64String(s);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    public static string? DecodeString(string value) =>
        TryDecode(value, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
}
