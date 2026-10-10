using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Services;

namespace WebSearchShortcut.SuggestionsProviders;

/// <summary>
/// Base class for providers that fetch a single HTTP response and parse it into suggestions.
/// Subclasses only describe the request URL and how to parse the body, which keeps the parsing testable offline.
/// </summary>
internal abstract class HttpSuggestionsProvider : ISuggestionsProvider
{
    public abstract string Name { get; }

    protected virtual string? UserAgent => null;

    protected abstract string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut);

    protected abstract IReadOnlyList<Suggestion> ParseResponse(string body, string query);

    public async Task<IReadOnlyList<Suggestion>> GetSuggestionsAsync(string query, WebSearchShortcutDataEntry shortcut, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = await SharedHttpClient
                .GetStringAsync(GetRequestUrl(query, shortcut), UserAgent, cancellationToken)
                .ConfigureAwait(false);

            return ParseResponse(body, query);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            // Network errors, timeouts and unexpected response shapes all degrade to "no suggestions".
            ExtensionHost.LogMessage($"{Name} suggestions failed: {e.Message}");

            return [];
        }
    }

    public override string ToString() => Name;
}
