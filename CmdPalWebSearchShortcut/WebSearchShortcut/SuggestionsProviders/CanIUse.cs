using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class CanIUse : HttpSuggestionsProvider
{
    public override string Name => "CanIUse";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://caniuse.com/process/query.php?search=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    internal static Suggestion[] Parse(string body)
    {
        using var json = JsonDocument.Parse(body);

        return [
            .. json.RootElement
                .GetProperty("featureIds")
                .EnumerateArray()
                .Take(10)
                .Select(o => o.GetString())
                .Where(s => s is not null)
                .Select(s => new Suggestion(s!))
        ];
    }
}
