using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebSearchShortcut.Services;

/// <param name="Entries">Valid shortcuts found in the input, in order.</param>
/// <param name="InvalidCount">Entries that were skipped because they lack a name or URL, or have an invalid keyword.</param>
internal sealed record ImportParseResult(IReadOnlyList<WebSearchShortcutDataEntry> Entries, int InvalidCount);

internal sealed record ImportMergeResult(int Added, int Updated, int Skipped)
{
    public IReadOnlyList<WebSearchShortcutDataEntry> Changed { get; init; } = [];
}

/// <summary>
/// Parses shortcut JSON in any supported shape and merges it into an existing list. Pure logic, no I/O.
/// </summary>
/// <remarks>
/// Supported inputs:
/// <list type="bullet">
/// <item><c>{ "Data": [ {...}, ... ] }</c> — this extension's shortcuts file and export format</item>
/// <item><c>[ {...}, ... ]</c> — a list of shortcuts</item>
/// <item><c>{ "Name": ..., "Url": ... }</c> — a single shortcut</item>
/// <item><c>{ "Google": { "Url": ... }, ... }</c> — the PowerToys Run WebSearchShortcut plugin's config, keyed by name</item>
/// </list>
/// Property names are matched case-insensitively.
/// </remarks>
internal static class ShortcutImporter
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Import input is either JSON or a path to a JSON file. Anything not starting with '{' or '[' is a path;
    /// surrounding quotes (as added by Explorer's "Copy as path") are removed.
    /// </summary>
    public static bool TryGetFilePath(string input, out string path)
    {
        path = input.Trim();

        if (path.StartsWith('{') || path.StartsWith('['))
            return false;

        if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
            path = path[1..^1].Trim();

        return path.Length > 0;
    }

    /// <exception cref="JsonException">The input isn't valid JSON.</exception>
    public static ImportParseResult Parse(string json)
    {
        var root = JsonNode.Parse(json, documentOptions: DocumentOptions);

        List<WebSearchShortcutDataEntry> entries = [];
        int invalid = 0;

        void AddCandidate(JsonNode? node, string? fallbackName)
        {
            var entry = node is JsonObject obj ? ReadEntry(obj, fallbackName) : null;

            if (entry is null)
                invalid++;
            else
                entries.Add(entry);
        }

        switch (root)
        {
            case JsonArray array:
                foreach (var item in array)
                    AddCandidate(item, null);
                break;

            case JsonObject obj when GetProperty(obj, "Data") is JsonArray data:
                foreach (var item in data)
                    AddCandidate(item, null);
                break;

            // A string "Url" means a single shortcut; a legacy shortcut that happens to be *named* "Url" has an object value.
            case JsonObject obj when StringValue(GetProperty(obj, "Url")) is not null || GetProperty(obj, "Urls") is JsonArray:
                AddCandidate(obj, null);
                break;

            case JsonObject obj:
                // PowerToys Run format: the property name is the shortcut name.
                foreach (var (name, value) in obj)
                    AddCandidate(value, name);
                break;
        }

        return new ImportParseResult(entries, invalid);
    }

    /// <summary>
    /// Merges <paramref name="incoming"/> into <paramref name="target"/>. A shortcut whose name already exists
    /// (case-insensitive) is skipped, or, with <paramref name="replaceExisting"/>, its values are copied onto the
    /// existing entry so it keeps its Id.
    /// </summary>
    public static ImportMergeResult Merge(List<WebSearchShortcutDataEntry> target, IEnumerable<WebSearchShortcutDataEntry> incoming, bool replaceExisting)
    {
        int added = 0, updated = 0, skipped = 0;
        List<WebSearchShortcutDataEntry> changed = [];

        foreach (var entry in incoming)
        {
            var existing = target.Find(e => string.Equals(e.Name, entry.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                if (!replaceExisting)
                {
                    skipped++;
                    continue;
                }

                existing.CopyValuesFrom(entry);
                updated++;
                changed.Add(existing);
                continue;
            }

            // Keep the imported Id (e.g. restoring an export) unless it would clash with another shortcut.
            if (string.IsNullOrWhiteSpace(entry.Id) || target.Exists(e => e.Id == entry.Id))
                entry.Id = string.Empty;

            target.Add(entry);
            added++;
            changed.Add(entry);
        }

        return new ImportMergeResult(added, updated, skipped) { Changed = changed };
    }

    private static WebSearchShortcutDataEntry? ReadEntry(JsonObject obj, string? fallbackName)
    {
        var name = GetString(obj, "Name") ?? fallbackName?.Trim();
        var url = GetString(obj, "Url")
            ?? (GetProperty(obj, "Urls") as JsonArray)?.Select(StringValue).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u))?.Trim();
        var keyword = GetString(obj, "Keyword");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url) || !WebSearchShortcutDataEntry.IsValidKeyword(keyword))
            return null;

        return new WebSearchShortcutDataEntry
        {
            Id = GetString(obj, "Id") ?? string.Empty,
            Name = name,
            Url = url,
            Keyword = keyword,
            SuggestionProvider = NormalizeProvider(GetString(obj, "SuggestionProvider")),
            HomePage = GetString(obj, "HomePage"),
            IconUrl = GetString(obj, "IconUrl"),
            // Whitespace is meaningful here (e.g. "%20" vs " "), so don't trim it.
            ReplaceWhitespace = StringValue(GetProperty(obj, "ReplaceWhitespace")) is { Length: > 0 } rw ? rw : null,
            BrowserPath = GetString(obj, "BrowserPath"),
            BrowserArgs = GetString(obj, "BrowserArgs"),
            Enabled = GetBool(obj, "Enabled") ?? true,
            UseAsFallback = GetBool(obj, "UseAsFallback") ?? true,
        };
    }

    /// <summary>Reads a JSON boolean, also accepting "true"/"false" strings; null when absent or anything else.</summary>
    private static bool? GetBool(JsonObject obj, string name)
    {
        if (GetProperty(obj, name) is not JsonValue value)
            return null;

        if (value.TryGetValue<bool>(out var b))
            return b;

        return value.TryGetValue<string>(out var s) && bool.TryParse(s.Trim(), out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Maps a provider name to its registered spelling ("google" → "Google"); unknown providers are dropped.
    /// </summary>
    private static string? NormalizeProvider(string? provider) =>
        provider is not null && SuggestionsRegistry.TryGet(provider, out var p) ? p.Name : null;

    private static JsonNode? GetProperty(JsonObject obj, string name)
    {
        foreach (var (key, value) in obj)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return null;
    }

    private static string? StringValue(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    /// <summary>Trimmed string value, or null when missing, not a string, or blank.</summary>
    private static string? GetString(JsonObject obj, string name) =>
        StringValue(GetProperty(obj, name))?.Trim() is { Length: > 0 } s ? s : null;
}
