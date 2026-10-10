using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class DuckDuckGo : HttpSuggestionsProvider
{
    public override string Name => "DuckDuckGo";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://duckduckgo.com/ac/?q=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body, query);

    internal static Suggestion[] Parse(string body, string query)
    {
        using var json = JsonDocument.Parse(body);

        return [
            .. json.RootElement
                .EnumerateArray()
                .Select(o => o.GetProperty("phrase").GetString())
                .Where(s =>
                    !string.IsNullOrEmpty(s) &&
                    !s.Equals(query, StringComparison.OrdinalIgnoreCase)
                )
                .Select(s => new Suggestion(s!))
        ];
    }
}
