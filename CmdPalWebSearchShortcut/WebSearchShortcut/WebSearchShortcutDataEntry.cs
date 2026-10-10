using System;
using System.Net;
using System.Text.Json.Serialization;
using WebSearchShortcut.Services;

namespace WebSearchShortcut;

internal sealed class WebSearchShortcutDataEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public string Url { get; set; } = string.Empty;
    // public string[]? Urls { get; set; }
    public string? SuggestionProvider { get; set; }
    public string? HomePage { get; set; }
    // public bool HideWhenEmptyQuery { get; set; } // TODO: wait powertoys cmdpal support for update with query
    public string? IconUrl { get; set; }
    public string? ReplaceWhitespace { get; set; }
    public string? BrowserPath { get; set; }
    public string? BrowserArgs { get; set; }

    /// <summary>When false the shortcut is hidden from Command Palette (top-level, fallback and keyword) but kept.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the shortcut offers to search for anything typed in Command Palette. When false it is only searched
    /// through its own page or its keyword.
    /// </summary>
    public bool UseAsFallback { get; set; } = true;

    /// <summary>
    /// Scheme and authority of <see cref="Url"/> (e.g. "https://www.google.com"), or an empty string when the URL can't be parsed.
    /// </summary>
    [JsonIgnore]
    public string Domain =>
        UriHelper.GetUri((Url ?? string.Empty).Split(' ')[0].Split('?')[0])?.GetLeftPart(UriPartial.Authority) ?? string.Empty;

    /// <summary>
    /// Copies every user-editable value from <paramref name="source"/>, keeping this entry's Id
    /// (Command Palette keys aliases, hotkeys and fallback settings on it).
    /// </summary>
    public void CopyValuesFrom(WebSearchShortcutDataEntry source)
    {
        Name = source.Name;
        Keyword = source.Keyword;
        Url = source.Url;
        SuggestionProvider = source.SuggestionProvider;
        HomePage = source.HomePage;
        IconUrl = source.IconUrl;
        ReplaceWhitespace = source.ReplaceWhitespace;
        BrowserPath = source.BrowserPath;
        BrowserArgs = source.BrowserArgs;
        Enabled = source.Enabled;
        UseAsFallback = source.UseAsFallback;
    }

    /// <summary>Case-insensitive match on name, keyword or URL, for the settings page search.</summary>
    public bool Matches(string filter) =>
        string.IsNullOrWhiteSpace(filter)
        || Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase)
        || (Keyword?.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)
        || Url.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A keyword is optional, but when set it must be a single word so "keyword query" can be split on the first space.
    /// </summary>
    static public bool IsValidKeyword(string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return true;

        foreach (var c in keyword)
        {
            if (char.IsWhiteSpace(c))
                return false;
        }

        return true;
    }

    static string UrlEncode(WebSearchShortcutDataEntry shortcut, string query)
    {
        string encoded = WebUtility.UrlEncode(query);

        return string.IsNullOrEmpty(shortcut.ReplaceWhitespace)
            ? encoded
            : encoded.Replace("+", shortcut.ReplaceWhitespace);
    }

    static public string GetSearchUrl(WebSearchShortcutDataEntry shortcut, string query)
    {
        string arguments = shortcut.Url.Replace("%s", UrlEncode(shortcut, query));

        return arguments;
    }

    /// <summary>
    /// Homepage to open for the shortcut; an empty string when neither HomePage nor Url yields a usable address.
    /// </summary>
    static public string GetHomePageUrl(WebSearchShortcutDataEntry shortcut)
    {
        return !string.IsNullOrWhiteSpace(shortcut.HomePage)
                ? shortcut.HomePage.Trim()
                : shortcut.Domain;
    }

    /// <summary>
    /// Checks whether <paramref name="query"/> starts with the shortcut's keyword ("g" matches "g" and "g cats", not "go").
    /// On a match, <paramref name="remainder"/> is the query without the keyword.
    /// </summary>
    static public bool TryStripKeyword(WebSearchShortcutDataEntry shortcut, string query, out string remainder)
    {
        remainder = query;

        var keyword = shortcut.Keyword?.Trim();
        if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(query))
            return false;

        var trimmedQuery = query.TrimStart();
        if (!trimmedQuery.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        if (trimmedQuery.Length == keyword.Length)
        {
            remainder = string.Empty;
            return true;
        }

        if (!char.IsWhiteSpace(trimmedQuery[keyword.Length]))
            return false;

        remainder = trimmedQuery[(keyword.Length + 1)..].TrimStart();
        return true;
    }
}
