using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class Npm : HttpSuggestionsProvider
{
    public override string Name => "Npm";

    // Use the public registry API instead of the website internal API to avoid bot protection issues
    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        "https://registry.npmjs.org/-/v1/search?text=" + Uri.EscapeDataString(query);

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    internal static Suggestion[] Parse(string body)
    {
        using var json = JsonDocument.Parse(body);

        return [
            .. json.RootElement
                .GetProperty("objects")
                .EnumerateArray()
                .Select(o =>
                {
                    var pkg = o.GetProperty("package");
                    var title = pkg.GetProperty("name").GetString();
                    var description = pkg.TryGetProperty("description", out var desc) ? desc.GetString() : "";
                    var version = pkg.TryGetProperty("version", out var ver) ? ver.GetString() : null;
                    return title is null
                        ? null
                        : new Suggestion(title, description ?? "", version is null ? null : [version]);
                })
                .Where(s => s is not null)
                .Select(s => s!)
        ];
    }
}
