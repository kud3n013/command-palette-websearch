using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.Services;

/// <summary>
/// Owns the shortcuts file: loading, saving, and every add/edit/delete/import, for both the palette commands and
/// the settings page. All mutations are serialized by a lock and announced through <see cref="Changed"/>.
/// </summary>
internal sealed class ShortcutService(string filePath)
{
    private readonly Lock _lock = new();
    private Storage? _storage;

    public string FilePath { get; } = filePath;

    /// <summary>Why the shortcuts file couldn't be loaded, or null when it loaded fine.</summary>
    public Exception? LoadError { get; private set; }

    /// <summary>
    /// Raised after shortcuts are added, edited, enabled/disabled, removed, imported, reset or reloaded.
    /// <see cref="ShortcutsChangedEventArgs.ListChanged"/> tells whether shortcuts were added or removed, or only edited.
    /// </summary>
    public event EventHandler<ShortcutsChangedEventArgs>? Changed;

    /// <summary>A snapshot of the shortcuts; empty when the file couldn't be loaded.</summary>
    public IReadOnlyList<WebSearchShortcutDataEntry> GetShortcuts()
    {
        lock (_lock)
        {
            EnsureLoaded();

            return _storage is null ? [] : [.. _storage.Data];
        }
    }

    /// <returns>False when the shortcuts file couldn't be loaded, so nothing was saved.</returns>
    public bool Add(WebSearchShortcutDataEntry shortcut)
    {
        lock (_lock)
        {
            EnsureLoaded();
            if (_storage is null)
                return false;

            _storage.Data.Add(shortcut);
            Save();
        }

        ExtensionHost.LogMessage($"Added shortcut ({shortcut.Name},{shortcut.Url})");
        RaiseChanged(listChanged: true);
        FetchMissingIcons([shortcut]);

        return true;
    }

    /// <summary>Copies <paramref name="values"/> onto the shortcut with Id <paramref name="id"/>.</summary>
    /// <returns>False when no such shortcut exists (e.g. it was deleted meanwhile) or the file couldn't be loaded.</returns>
    public bool Update(string id, WebSearchShortcutDataEntry values)
    {
        WebSearchShortcutDataEntry? existing;

        lock (_lock)
        {
            EnsureLoaded();
            existing = _storage?.Data.Find(e => e.Id == id);
            if (existing is null)
                return false;

            // The forms don't edit IconUrl: keep the cached icon unless the site changed.
            values.IconUrl ??= existing.Domain == values.Domain ? existing.IconUrl : null;
            // Nor Enabled, which only SetEnabled changes.
            values.Enabled = existing.Enabled;

            existing.CopyValuesFrom(values);
            Save();
        }

        ExtensionHost.LogMessage($"Edited shortcut ({existing.Name},{existing.Url})");
        RaiseChanged(listChanged: false);
        FetchMissingIcons([existing]);

        return true;
    }

    /// <returns>False when no such shortcut exists or the file couldn't be loaded.</returns>
    public bool SetEnabled(string id, bool enabled)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var existing = _storage?.Data.Find(e => e.Id == id);
            if (existing is null)
                return false;

            if (existing.Enabled == enabled)
                return true;

            existing.Enabled = enabled;
            Save();
        }

        ExtensionHost.LogMessage($"{(enabled ? "Enabled" : "Disabled")} shortcut ({id})");
        RaiseChanged(listChanged: false);

        return true;
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            if (_storage is null || _storage.Data.RemoveAll(e => e.Id == id) == 0)
                return false;

            Save();
        }

        ExtensionHost.LogMessage($"Deleted shortcut ({id})");
        RaiseChanged(listChanged: true);

        return true;
    }

    /// <returns>The merge counts, or null when the shortcuts file couldn't be loaded.</returns>
    public ImportMergeResult? Import(IEnumerable<WebSearchShortcutDataEntry> shortcuts, bool replaceExisting)
    {
        ImportMergeResult result;

        lock (_lock)
        {
            EnsureLoaded();
            if (_storage is null)
                return null;

            result = ShortcutImporter.Merge(_storage.Data, shortcuts, replaceExisting);

            if (result.Changed.Count > 0)
                Save();
        }

        ExtensionHost.LogMessage($"Imported shortcuts: {result.Added} added, {result.Updated} updated, {result.Skipped} skipped");

        if (result.Changed.Count > 0)
        {
            RaiseChanged(listChanged: true);
            FetchMissingIcons(result.Changed);
        }

        return result;
    }

    /// <summary>All shortcuts in the shortcuts-file format, which <see cref="ShortcutImporter"/> reads back.</summary>
    public string ExportJson()
    {
        lock (_lock)
        {
            EnsureLoaded();

            return Storage.ToJson(new Storage { Data = _storage is null ? [] : [.. _storage.Data] });
        }
    }

    /// <summary>Backs up the current file and replaces it with the default shortcuts.</summary>
    /// <returns>The backup path, or null if there was no file.</returns>
    public string? ResetToDefaults()
    {
        string? backupPath;

        lock (_lock)
        {
            backupPath = Storage.ResetToDefaults(FilePath);
            _storage = null;
            EnsureLoaded();
        }

        RaiseChanged(listChanged: true);

        return backupPath;
    }

    /// <summary>Re-reads the file from disk, e.g. after the user fixed it by hand.</summary>
    public void Reload()
    {
        lock (_lock)
        {
            _storage = null;
            EnsureLoaded();
        }

        RaiseChanged(listChanged: true);
    }

    // Must be called under _lock.
    private void EnsureLoaded()
    {
        if (_storage is not null)
            return;

        try
        {
            _storage = Storage.ReadFromFile(FilePath);
            LoadError = null;
        }
        catch (Exception ex)
        {
            LoadError = ex;
            ExtensionHost.LogMessage($"Error loading shortcuts: {ex}");
        }
    }

    // Must be called under _lock with _storage loaded.
    private void Save() => Storage.WriteToFile(FilePath, _storage!);

    private void RaiseChanged(bool listChanged) => Changed?.Invoke(this, new ShortcutsChangedEventArgs(listChanged));

    /// <summary>
    /// Resolves and caches favicons in the background. Icons already render without this (from the URL),
    /// so failures are only logged.
    /// </summary>
    private void FetchMissingIcons(IReadOnlyList<WebSearchShortcutDataEntry> shortcuts)
    {
        var pending = shortcuts.Where(s => string.IsNullOrWhiteSpace(s.IconUrl)).ToArray();
        if (pending.Length == 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                Dictionary<WebSearchShortcutDataEntry, string> resolved = [];

                foreach (var shortcut in pending)
                {
                    var probe = new WebSearchShortcutDataEntry { Url = shortcut.Url };
                    resolved[shortcut] = await IconService.UpdateIconUrlAsync(probe).ConfigureAwait(false);
                }

                lock (_lock)
                {
                    // Only touch shortcuts that still exist and weren't given an icon meanwhile.
                    var applied = false;
                    foreach (var (shortcut, iconUrl) in resolved)
                    {
                        if (_storage?.Data.Contains(shortcut) == true && string.IsNullOrWhiteSpace(shortcut.IconUrl) && iconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            shortcut.IconUrl = iconUrl;
                            applied = true;
                        }
                    }

                    if (!applied)
                        return;

                    Save();
                }

                RaiseChanged(listChanged: false);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage($"Failed to update shortcut icons: {ex.Message}");
            }
        });
    }
}

internal sealed class ShortcutsChangedEventArgs(bool listChanged) : EventArgs
{
    /// <summary>True when shortcuts were added or removed (or everything was reloaded); false when only values changed.</summary>
    public bool ListChanged { get; } = listChanged;
}
