using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class Bing : HttpSuggestionsProvider
{
    public override string Name => "Bing";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://api.bing.com/qsonhs.aspx?q=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    internal static Suggestion[] Parse(string body)
    {
        using var json = JsonDocument.Parse(body);

        var root = json.RootElement.GetProperty("AS");

        if (root.GetProperty("FullResults").GetInt32() == 0)
            return [];

        return [
            .. root.GetProperty("Results")
                .EnumerateArray()
                .SelectMany(r => r.GetProperty("Suggests").EnumerateArray())
                .Select(s => s.GetProperty("Txt").GetString())
                .Where(s => s is not null)
                .Select(s => new Suggestion(s!))
        ];
    }
}
