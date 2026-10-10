// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Browser;
using WebSearchShortcut.Commands;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Setting;

namespace WebSearchShortcut;

public partial class WebSearchShortcutCommandsProvider : CommandProvider
{
    private readonly ShortcutService _service;
    private readonly ShortcutsSettingsPage _settingsPage;
    private readonly ICommandItem _addShortcutItem;
    private ICommandItem[] _topLevelCommands = [];
    private IFallbackCommandItem[] _fallbackCommands = [];

    public WebSearchShortcutCommandsProvider()
    {
        DisplayName = Resources.WebSearchShortcut_DisplayName;
        Icon = Icons.Logo;

        var logger = new CmdPalLogger("WebSearchShortcut")
        {
            LogLevel_ = SettingsManager.Instance.LogLevel_
        };

        DefaultBrowserProvider.Logger = logger;
        BrowsersDiscovery.Logger = logger;

        SettingsManager.Instance.Settings.SettingsChanged += (sender, eventArgs) =>
        {
            logger.LogLevel_ = SettingsManager.Instance.LogLevel_;
        };

        _service = new ShortcutService(GetShortcutsJsonPath());
        _service.Changed += (_, _) =>
        {
            ReloadCommands();
            RaiseItemsChanged(0);
        };

        // Shortcut management lives in the settings page, shown both in Command Palette Settings and from the palette.
        _settingsPage = new ShortcutsSettingsPage(_service);
        Settings = new ShortcutsSettings(_settingsPage);

        var addShortcutPage = new AddShortcutPage(null, _service)
        {
            Name = Resources.AddShortcutItem_Name
        };
        _addShortcutItem = new CommandItem(addShortcutPage)
        {
            Title = Resources.AddShortcutItem_Title,
            Icon = Icons.AddShortcut,
            MoreCommands = [new CommandContextItem(_settingsPage) { Title = Resources.SettingsItem_Title }]
        };
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (_topLevelCommands.Length == 0)
        {
            ReloadCommands();
        }

        return _topLevelCommands;
    }

    public override IFallbackCommandItem[] FallbackCommands() => _fallbackCommands;

    internal static string GetShortcutsJsonPath()
    {
        string directory = Utilities.BaseSettingsPath("WebSearchShortcut");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "WebSearchShortcut.json");
    }

    private void ReloadCommands()
    {
        var shortcuts = _service.GetShortcuts();

        List<ICommandItem> items = [_addShortcutItem];
        List<IFallbackCommandItem> fallbackItem = [];

        if (_service.LoadError is { } loadError)
        {
            items.Insert(0, CreateLoadErrorItem(loadError));
        }
        else
        {
            // Disabled shortcuts stay in the file (and the settings page) but not in the palette.
            var enabled = shortcuts.Where(shortcut => shortcut.Enabled).ToArray();
            items.AddRange(enabled.Select(CreateCommandItem));
            // Without "global fallback" a shortcut still answers to its keyword, which is also handled by its fallback item.
            fallbackItem.AddRange(enabled
                .Where(shortcut => shortcut.UseAsFallback || !string.IsNullOrWhiteSpace(shortcut.Keyword))
                .Select(shortcut => new FallbackSearchWebItem(shortcut)));
        }

        _topLevelCommands = [.. items];
        _fallbackCommands = [.. fallbackItem];
    }

    private CommandItem CreateLoadErrorItem(Exception error)
    {
        var reloadCommand = new CommandContextItem(
            title: Resources.StorageLoadError_Reload,
            name: Resources.StorageLoadError_Reload,
            action: _service.Reload,
            result: CommandResult.KeepOpen()
        )
        {
            Icon = new IconInfo("")
        };

        var confirmResetCommand = new AnonymousCommand(() =>
        {
            var backupPath = _service.ResetToDefaults();

            if (backupPath is not null)
            {
                new ToastStatusMessage(StringFormatter.Format(Resources.StorageReset_ToastTemplate, new() { ["backup"] = backupPath })).Show();
            }
        })
        {
            Name = Resources.StorageLoadError_Reset,
            Result = CommandResult.KeepOpen()
        };

        var resetCommand = new CommandContextItem(
            title: Resources.StorageLoadError_Reset,
            name: Resources.StorageLoadError_Reset,
            action: () => { },
            result: CommandResult.Confirm(new ConfirmationArgs
            {
                Title = Resources.StorageLoadError_ResetConfirmTitle,
                Description = Resources.StorageLoadError_ResetConfirmDescription,
                PrimaryCommand = confirmResetCommand,
                IsPrimaryCommandCritical = true
            })
        )
        {
            Icon = Icons.Delete,
            IsCritical = true
        };

        var openFileCommand = new AnonymousCommand(() => ShellHelpers.OpenInShell(_service.FilePath))
        {
            Name = Resources.StorageLoadError_OpenFile,
            Result = CommandResult.Dismiss()
        };

        return new CommandItem(openFileCommand)
        {
            Title = Resources.StorageLoadError_Title,
            Subtitle = error is StorageLoadException ? error.Message : $"{_service.FilePath}: {error.Message}",
            Icon = new IconInfo(""),
            MoreCommands = [reloadCommand, resetCommand]
        };
    }

    private CommandItem CreateCommandItem(WebSearchShortcutDataEntry shortcut)
    {
        var openHomepageCommand = new CommandContextItem(
            new OpenHomePageCommand(shortcut)
            {
                Name = StringFormatter.Format(Resources.OpenHomepageItem_NameTemplate, new() { ["shortcut"] = shortcut.Name })
            }
        )
        {
            Title = StringFormatter.Format(Resources.OpenHomepageItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Icon = Icons.Home
        };

        var editShortcutPage = new AddShortcutPage(shortcut, _service)
        {
            Name = StringFormatter.Format(Resources.EditShortcutItem_NameTemplate, new() { ["shortcut"] = shortcut.Name }),
        };
        var editCommand = new CommandContextItem(editShortcutPage)
        {
            Title = StringFormatter.Format(Resources.EditShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Icon = Icons.Edit
        };

        var confirmDeleteCommand = new AnonymousCommand(() => _service.Delete(shortcut.Id))
        {
            Name = Resources.DeleteShortcut_ConfirmButton,
            Result = CommandResult.KeepOpen()
        };

        var deleteCommand = new CommandContextItem(
            title: StringFormatter.Format(Resources.DeleteShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            name: $"[UNREACHABLE] DeleteCommand.Name - shortcut='{shortcut.Name}'",
            action: () => { },
            result: CommandResult.Confirm(new ConfirmationArgs
            {
                Title = StringFormatter.Format(Resources.DeleteShortcut_ConfirmTitleTemplate, new() { ["shortcut"] = shortcut.Name }),
                Description = Resources.DeleteShortcut_ConfirmDescription,
                PrimaryCommand = confirmDeleteCommand,
                IsPrimaryCommandCritical = true
            })
        )
        {
            Icon = Icons.Delete,
            IsCritical = true
        };

        var manageCommand = new CommandContextItem(_settingsPage)
        {
            Title = Resources.SettingsItem_Title
        };

        var commandItem = new CommandItem(
            new SearchWebPage(shortcut)
            {
                Name = StringFormatter.Format(Resources.ShortcutItem_NameTemplate, new() { ["shortcut"] = shortcut.Name })
            }
        )
        {
            Title = StringFormatter.Format(Resources.ShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            // Top-level items have no tags, so surface the keyword as a usage hint: "Search Google (g …)".
            Subtitle = StringFormatter.Format(Resources.ShortcutItem_SubtitleTemplate, new() { ["shortcut"] = shortcut.Name })
                + (string.IsNullOrWhiteSpace(shortcut.Keyword) ? string.Empty : $" ({shortcut.Keyword.Trim()} …)"),
            Icon = IconService.GetIconInfo(shortcut),
            MoreCommands = [openHomepageCommand, editCommand, deleteCommand, manageCommand]
        };

        return commandItem;
    }
}
