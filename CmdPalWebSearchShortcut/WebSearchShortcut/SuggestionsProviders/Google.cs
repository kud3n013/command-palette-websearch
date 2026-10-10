using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class Google : HttpSuggestionsProvider
{
    public override string Name => "Google";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://www.google.com/complete/search?output=chrome&q=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    internal static Suggestion[] Parse(string body)
    {
        using var json = JsonDocument.Parse(body);

        var results = json.RootElement.EnumerateArray().ElementAt(1);

        return [
            .. results
                .EnumerateArray()
                .Select(o => o.GetString())
                .Where(s => s is not null)
                .Select(s => new Suggestion(s!))
        ];
    }
}
