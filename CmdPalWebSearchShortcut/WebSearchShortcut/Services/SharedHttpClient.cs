using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebSearchShortcut.Services;

/// <summary>
/// Single HttpClient shared by suggestion providers and the icon service.
/// Reusing one instance avoids socket exhaustion; the timeout keeps a slow endpoint from hanging a request forever.
/// </summary>
internal static class SharedHttpClient
{
    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(5);

    public static HttpClient Instance { get; } = new() { Timeout = Timeout };

    /// <summary>
    /// Sends a GET request, optionally with a per-request User-Agent, and returns the body as a string.
    /// </summary>
    public static async Task<string> GetStringAsync(string url, string? userAgent, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(userAgent))
        {
            request.Headers.UserAgent.ParseAdd(userAgent);
        }

        using var response = await Instance
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
