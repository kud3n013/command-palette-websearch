using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebSearchShortcut.Services;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed partial class Wikipedia : HttpSuggestionsProvider
{
    private const string DefaultLanguage = "en";

    public override string Name => "Wikipedia";

    protected override string? UserAgent => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

    protected override string GetRequestUrl(string query, WebSearchShortcutDataEntry shortcut) =>
        $"https://{GetLanguage(shortcut.Url)}.wikipedia.org/w/rest.php/v1/search/title?limit=10&q={Uri.EscapeDataString(query)}";

    protected override IReadOnlyList<Suggestion> ParseResponse(string body, string query) => Parse(body);

    // Matches language subdomains such as "en", "zh-yue", "simple" (optionally the mobile "*.m" host), but not "www".
    [GeneratedRegex(@"^(?!www\.)([a-z][a-z0-9-]*)(?:\.m)?\.wikipedia\.org$", RegexOptions.IgnoreCase)]
    private static partial Regex LanguageHostRegex();

    /// <summary>
    /// Returns the Wikipedia language of the shortcut's search URL (e.g. "fr" for https://fr.wikipedia.org/...),
    /// or "en" when the URL is not a language-specific wikipedia.org URL.
    /// </summary>
    internal static string GetLanguage(string? url)
    {
        var host = UriHelper.GetUri(url?.Split('?')[0] ?? string.Empty)?.Host;
        if (host is null)
            return DefaultLanguage;

        var match = LanguageHostRegex().Match(host);

        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : DefaultLanguage;
    }

    internal static Suggestion[] Parse(string body)
    {
        using var json = JsonDocument.Parse(body);

        return [
            .. json.RootElement
                .GetProperty("pages")
                .EnumerateArray()
                .Select(o =>
                {
                    var title = o.TryGetProperty("title", out var t) ? t.GetString() : null;
                    var description = o.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                    string? image = null;

                    if (o.TryGetProperty("thumbnail", out var thumb) && thumb.ValueKind == JsonValueKind.Object)
                    {
                        if (thumb.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
                        {
                            var urlStr = url.GetString();
                            if (!string.IsNullOrEmpty(urlStr))
                            {
                                image = urlStr.StartsWith("//", StringComparison.Ordinal) ? "https:" + urlStr : urlStr;
                            }
                        }
                    }

                    return (Title: title, Description: description, Image: image);
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .Select(p => new Suggestion(
                    Title: p.Title!,
                    Description: p.Description ?? "",
                    Image: p.Image
                ))
        ];
    }
}
