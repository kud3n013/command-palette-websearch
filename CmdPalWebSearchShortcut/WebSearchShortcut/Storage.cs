using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WebSearchShortcut.Helpers;

namespace WebSearchShortcut;

internal sealed class Storage
{
    public List<WebSearchShortcutDataEntry> Data { get; set; } = [];

    // public List<WebSearchShortcutDataEntry> GetTopLevelShortcuts()
    // {
    //     return Data.FindAll(x => !x.HideWhenEmptyQuery);
    // }

    // public List<WebSearchShortcutDataEntry> GetFallbackShortcuts()
    // {
    //     return Data.FindAll(x => x.HideWhenEmptyQuery);
    // }

    /// <summary>
    /// Loads shortcuts from <paramref name="path"/>, creating the file with default shortcuts if it doesn't exist.
    /// </summary>
    /// <exception cref="StorageLoadException">The file exists but isn't valid shortcut JSON. The file is left untouched.</exception>
    public static Storage ReadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            var defaultStorage = CreateDefault();
            WriteToFile(path, defaultStorage);

            return defaultStorage;
        }

        var jsonStringReading = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(jsonStringReading))
        {
            return new Storage();
        }

        Storage data;
        try
        {
            data = JsonSerializer.Deserialize(jsonStringReading, AppJsonSerializerContext.Default.Storage) ?? new Storage();
        }
        catch (JsonException ex)
        {
            throw new StorageLoadException(path, ex);
        }

        bool modified = Sanitize(data);
        modified |= EnsureIds(data.Data);

        if (modified)
        {
            WriteToFile(path, data);
        }

        return data;
    }

    /// <summary>
    /// Replaces the file at <paramref name="path"/> with the default shortcuts, first copying any existing file to a timestamped .bak next to it.
    /// </summary>
    /// <returns>The backup path, or null if there was no file to back up.</returns>
    public static string? ResetToDefaults(string path)
    {
        string? backupPath = null;

        if (File.Exists(path))
        {
            backupPath = $"{path}.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(path, backupPath, overwrite: true);
        }

        WriteToFile(path, CreateDefault());

        return backupPath;
    }

    private static Storage CreateDefault()
    {
        var storage = new Storage();
        storage.Data.AddRange(GetDefaultEntries());

        return storage;
    }

    /// <summary>
    /// Repairs values that are valid JSON but would break the extension later (null entries, null required fields).
    /// </summary>
    private static bool Sanitize(Storage data)
    {
        bool modified = false;

        if (data.Data is null)
        {
            data.Data = [];
            return true;
        }

        modified |= data.Data.RemoveAll(entry => entry is null) > 0;

        foreach (var entry in data.Data)
        {
            if (entry.Name is null) { entry.Name = string.Empty; modified = true; }
            if (entry.Url is null) { entry.Url = string.Empty; modified = true; }
        }

        return modified;
    }

    private static List<WebSearchShortcutDataEntry> GetDefaultEntries()
    {
        return
        [
            new WebSearchShortcutDataEntry
            {
                Name = "Google",
                Url = "https://www.google.com/search?q=%s",
                SuggestionProvider = "Google",
            },
            new WebSearchShortcutDataEntry
            {
                Name = "Bing",
                Url = "https://www.bing.com/search?q=%s",
                SuggestionProvider = "Bing",
            },
            new WebSearchShortcutDataEntry
            {
                Name = "Youtube",
                Url = "https://www.youtube.com/results?search_query=%s",
                SuggestionProvider = "YouTube"
            },
            new WebSearchShortcutDataEntry
            {
                Name = "DuckDuckGo",
                Url = "https://duckduckgo.com/?q=%s",
                SuggestionProvider = "DuckDuckGo",
                // HideWhenEmptyQuery = true
            },
            new WebSearchShortcutDataEntry
            {
                Name = "Wikipedia",
                Url = "https://en.wikipedia.org/w/index.php?fulltext=1&search=%s",
                SuggestionProvider = "Wikipedia",
                // HideWhenEmptyQuery = true
            },
            new WebSearchShortcutDataEntry
            {
                Name = "npm",
                Url = "https://www.npmjs.com/search?q=%s",
                SuggestionProvider = "Npm",
                // HideWhenEmptyQuery = true
            }
        ];
    }

    public static void WriteToFile(string path, Storage data)
    {
        File.WriteAllText(path, ToJson(data));
    }

    /// <summary>
    /// Serializes to the same format as the shortcuts file, which is also the export/import format.
    /// </summary>
    public static string ToJson(Storage data)
    {
        EnsureIds(data.Data);

        return JsonPrettyFormatter.ToPrettyJson(data, AppJsonSerializerContext.Default.Storage);
    }

    private static bool EnsureIds(List<WebSearchShortcutDataEntry> shortcuts)
    {
        bool modified = false;
        HashSet<string> existingIds = [];

        foreach (var shortcut in shortcuts)
        {
            while (string.IsNullOrWhiteSpace(shortcut.Id) || existingIds.Contains(shortcut.Id))
            {
                modified = true;

                shortcut.Id = GenerateId();
            }
            existingIds.Add(shortcut.Id);
        }

        return modified;
    }

    private static string GenerateId()
    {
        byte[] buffer = new byte[8];
        Random.Shared.NextBytes(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);

        return $"WebSearchShortcut{randomNumber}";
    }
}

/// <summary>
/// Thrown when the shortcuts file exists but can't be parsed.
/// </summary>
internal sealed class StorageLoadException(string filePath, Exception innerException)
    : Exception($"Invalid shortcuts file '{filePath}': {innerException.Message}", innerException)
{
    public string FilePath { get; } = filePath;
}
