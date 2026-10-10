using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;

namespace WebSearchShortcut;

/// <summary>
/// Settings page card shown instead of the shortcut list when the shortcuts file can't be loaded.
/// </summary>
internal sealed partial class LoadErrorForm : FormContent
{
    private const string OpenAction = "open";
    private const string ReloadAction = "reload";
    private const string ResetAction = "reset";
    private const string ConfirmResetAction = "confirmReset";

    private readonly ShortcutService _service;
    private readonly Action<string> _showNotice;
    private bool _confirmingReset;

    /// <param name="showNotice">Shows a message at the top of the page; this card disappears once the file loads.</param>
    public LoadErrorForm(ShortcutService service, Action<string> showNotice)
    {
        _service = service;
        _showNotice = showNotice;
        Render();
    }

    /// <summary>Refreshes the error text, e.g. after a reload that failed again.</summary>
    public void Sync()
    {
        _confirmingReset = false;
        Render();
    }

    public override CommandResult SubmitForm(string inputs, string data)
    {
        switch (AdaptiveCardHelpers.GetSubmitAction(inputs, data))
        {
            case OpenAction:
                ShellHelpers.OpenInShell(_service.FilePath);
                break;

            case ReloadAction:
                _service.Reload();
                break;

            case ResetAction:
                _confirmingReset = true;
                Render();
                break;

            case ShortcutFormTemplate.CancelAction:
                _confirmingReset = false;
                Render();
                break;

            case ConfirmResetAction:
                var backupPath = _service.ResetToDefaults();
                if (backupPath is not null)
                    _showNotice(StringFormatter.Format(Resources.StorageReset_ToastTemplate, new() { ["backup"] = backupPath }));
                break;
        }

        return CommandResult.KeepOpen();
    }

    private void Render()
    {
        if (_confirmingReset)
        {
            TemplateJson = CardTemplates.Card(
            [
                CardTemplates.Text(Resources.StorageLoadError_ResetConfirmTitle, bold: true),
                CardTemplates.Text(Resources.StorageLoadError_ResetConfirmDescription, subtle: true),
            ],
            [
                CardTemplates.Submit(Resources.Settings_ResetConfirmButton, ConfirmResetAction, validateInputs: false, style: "destructive"),
                CardTemplates.Submit(Resources.Settings_Cancel, ShortcutFormTemplate.CancelAction, validateInputs: false),
            ]);

            return;
        }

        var error = _service.LoadError;
        List<string> body =
        [
            CardTemplates.Heading(Resources.StorageLoadError_Title),
            CardTemplates.Status(error is StorageLoadException ? error.Message : $"{_service.FilePath}: {error?.Message}", isError: true),
        ];

        TemplateJson = CardTemplates.Card(body,
        [
            CardTemplates.Submit(Resources.StorageLoadError_OpenFile, OpenAction, validateInputs: false),
            CardTemplates.Submit(Resources.StorageLoadError_Reload, ReloadAction, validateInputs: false),
            CardTemplates.Submit(Resources.StorageLoadError_Reset, ResetAction, validateInputs: false, style: "destructive"),
        ]);
    }
}
