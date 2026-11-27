using System.Net;
using System.Text;

namespace Trainer.Web.Core.Simulation;

/// <summary>
/// A simulated network: an <see cref="HttpMessageHandler"/> that answers for a
/// fixed set of hosts, so missions can make "outbound" requests without touching the real network.
/// Hosts can have DNS aliases. Unknown hosts fail like a failed DNS lookup. Redirects are
/// returned as-is; following them is up to the caller.
/// </summary>
public sealed class FakeNetwork : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds a host. The handler gets every request for it, any scheme or port.</summary>
    public FakeNetwork Host(string host, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _hosts[host] = handler;
        return this;
    }

    /// <summary>A DNS name that resolves to another host.</summary>
    public FakeNetwork Alias(string name, string target)
    {
        _aliases[name] = target;
        return this;
    }

    /// <summary>The host a name resolves to (itself if it has no alias).</summary>
    public string Resolve(string host) => _aliases.GetValueOrDefault(host, host);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI required.");
        if (!_hosts.TryGetValue(Resolve(uri.Host), out var handler))
            throw new HttpRequestException($"No such host is known. ({uri.Host}:{uri.Port})");

        var response = handler(request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }

    public static HttpResponseMessage Html(string html) =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") };

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Text(string text) =>
        new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "text/plain") };

    public static HttpResponseMessage NotFound() =>
        new(HttpStatusCode.NotFound) { Content = new StringContent("Not found", Encoding.UTF8, "text/plain") };

    public static HttpResponseMessage Redirect(string location) =>
        new(HttpStatusCode.Found) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };
}
