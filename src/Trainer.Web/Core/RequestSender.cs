using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Trainer.Web.Core;

public sealed record ConsoleRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers, string? Body);

public sealed record ConsoleResponse(int StatusCode, string ReasonPhrase, IReadOnlyList<KeyValuePair<string, string>> Headers, string Body);

public sealed record ConsoleExchange(DateTimeOffset At, ConsoleRequest Request, ConsoleResponse? Response, string? Error);

/// <summary>
/// Backend of the request console: sends requests server-side to the
/// app's own address, never follows redirects, only allows <c>/api/missions/</c> paths,
/// and keeps the last 10 exchanges per lab in memory. Unless the player typed one,
/// an <c>Accept-Language</c> header for the current UI language is added on the
/// wire only, so lab data matches the EN | DE switch without cluttering the console.
/// </summary>
public sealed class RequestSender(IHttpClientFactory httpClientFactory, IServer server)
{
    public const string HttpClientName = "self";
    public const int HistorySize = 10;
    public static readonly string[] Methods = ["GET", "POST", "PUT", "DELETE"];

    private readonly ConcurrentDictionary<string, LinkedList<ConsoleExchange>> _history = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ConsoleExchange> History(string slug)
    {
        if (!_history.TryGetValue(slug, out var list)) return [];
        lock (list) return list.ToList();
    }

    public void ClearHistory(string slug) => _history.TryRemove(slug, out _);

    public async Task<ConsoleExchange> SendAsync(string slug, ConsoleRequest request, Language language = Language.En, CancellationToken ct = default)
    {
        ConsoleExchange exchange;
        try
        {
            exchange = new ConsoleExchange(DateTimeOffset.Now, request, await SendCoreAsync(request, language, ct), null);
        }
        catch (Exception ex) when (ex is ArgumentException or HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            exchange = new ConsoleExchange(DateTimeOffset.Now, request, null, ex.Message);
        }

        var list = _history.GetOrAdd(slug, _ => new LinkedList<ConsoleExchange>());
        lock (list)
        {
            list.AddFirst(exchange);
            while (list.Count > HistorySize) list.RemoveLast();
        }
        return exchange;
    }

    private async Task<ConsoleResponse> SendCoreAsync(ConsoleRequest request, Language language, CancellationToken ct)
    {
        var method = request.Method.Trim().ToUpperInvariant();
        if (!Methods.Contains(method))
            throw new ArgumentException($"Method '{request.Method}' is not allowed.");

        var baseAddress = GetBaseAddress();
        if (!TryBuildUri(baseAddress, request.Path, out var uri))
            throw new ArgumentException($"Only paths under {LabRegistry.ApiPrefix} are allowed.");

        using var message = new HttpRequestMessage(new HttpMethod(method), uri);
        if (!string.IsNullOrEmpty(request.Body) && method != "GET")
            message.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");

        foreach (var (name, value) in request.Headers)
        {
            if (string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase)) continue;
            if (message.Headers.TryAddWithoutValidation(name, value)) continue;
            if (message.Content is not null)
            {
                message.Content.Headers.Remove(name);
                message.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        if (!request.Headers.Keys.Any(k => string.Equals(k, "Accept-Language", StringComparison.OrdinalIgnoreCase)))
            message.Headers.TryAddWithoutValidation("Accept-Language", Languages.Code(language));

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var headers = response.Headers.Concat(response.Content.Headers)
            .Select(h => new KeyValuePair<string, string>(h.Key, string.Join(", ", h.Value)))
            .ToList();

        return new ConsoleResponse((int)response.StatusCode, response.ReasonPhrase ?? "", headers, PrettyPrint(body));
    }

    /// <summary>
    /// Resolves <paramref name="path"/> against the app's own address and accepts it
    /// only if it stays on that host and under <c>/api/missions/</c> after normalization.
    /// </summary>
    public static bool TryBuildUri(Uri baseAddress, string? path, out Uri uri)
    {
        uri = baseAddress;
        if (string.IsNullOrWhiteSpace(path)) return false;
        path = path.Trim();
        if (!path.StartsWith(LabRegistry.ApiPrefix, StringComparison.Ordinal)) return false;
        if (!Uri.TryCreate(baseAddress, path, out var candidate)) return false;
        if (Uri.Compare(candidate, baseAddress, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
            return false;
        if (!candidate.AbsolutePath.StartsWith(LabRegistry.ApiPrefix, StringComparison.Ordinal)) return false;
        uri = candidate;
        return true;
    }

    private Uri GetBaseAddress()
    {
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses
            .FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        if (address is null) return new Uri("http://localhost:5000/");

        var builder = new UriBuilder(address);
        if (builder.Host is "0.0.0.0" or "[::]" or "+" or "*") builder.Host = "localhost";
        return builder.Uri;
    }

    private static string PrettyPrint(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
