using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed partial class YouTube : HttpSuggestionsProvider
{
    public override string Name => "YouTube";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://suggestqueries-clients6.youtube.com/complete/search?ds=yt&client=youtube&gs_ri=youtube&q=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    [GeneratedRegex(@"window\.google\.ac\.h\((.*)\)$")]
    private static partial Regex JsonpRegex();

    internal static Suggestion[] Parse(string body)
    {
        // The endpoint answers with JSONP: window.google.ac.h([...])
        var match = JsonpRegex().Match(body);
        if (!match.Success)
            return [];

        using var json = JsonDocument.Parse(match.Groups[1].Value);

        return [
            .. json.RootElement[1]
                .EnumerateArray()
                .Select(o => o[0].GetString())
                .Where(s => s is not null)
                .Select(s => new Suggestion(s!))
        ];
    }
}
