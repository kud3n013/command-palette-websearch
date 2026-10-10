using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using static WebSearchShortcut.Helpers.AdaptiveCardHelpers;

namespace WebSearchShortcut;

/// <summary>
/// Settings page card for one shortcut. The header (icon, name, keyword, URL) has an on/off switch and expands to the
/// shortcut's fields with Save and Delete; deleting asks for confirmation inside the card.
/// </summary>
internal sealed partial class ShortcutCardForm : FormContent
{
    private const string ToggleEnabledAction = "toggleEnabled";
    private const string DeleteAction = "delete";
    private const string ConfirmDeleteAction = "confirmDelete";

    private readonly ShortcutService _service;
    private WebSearchShortcutDataEntry _shortcut;

    // Expanding happens in the card renderer, so the extension only knows the card is open when it rendered it open:
    // to show a validation error or the delete confirmation. While it is, Sync leaves it alone.
    private bool _expanded;
    private bool _confirmingDelete;
    private string? _status;
    private bool _statusIsError;

    public ShortcutCardForm(WebSearchShortcutDataEntry shortcut, ShortcutService service)
    {
        _shortcut = shortcut;
        _service = service;
        Render(values: null);
    }

    /// <summary>Called when the shortcut changed (e.g. its icon was fetched); re-renders unless the card is open.</summary>
    public void Sync(WebSearchShortcutDataEntry shortcut)
    {
        _shortcut = shortcut;

        if (!_expanded)
            Render(values: null);
    }

    public override CommandResult SubmitForm(string inputs, string data)
    {
        switch (GetSubmitAction(inputs, data))
        {
            case ToggleEnabledAction:
                _status = null;
                if (!_service.SetEnabled(_shortcut.Id, !_shortcut.Enabled))
                    SetStatus(Resources.Settings_ShortcutMissing, isError: true);
                // On success, the service's Changed event re-renders this card through Sync.
                Render(values: null);
                break;

            case ShortcutFormTemplate.SaveAction:
                Save(inputs);
                break;

            // These buttons don't send the inputs (so an empty required field can't block them): show the saved values.
            case DeleteAction:
                _expanded = true;
                _confirmingDelete = true;
                _status = null;
                Render(values: null);
                break;

            case ShortcutFormTemplate.CancelAction:
                _confirmingDelete = false;
                Render(values: null);
                break;

            case ConfirmDeleteAction:
                // On success the page drops this card when it rebuilds.
                if (!_service.Delete(_shortcut.Id))
                {
                    _confirmingDelete = false;
                    SetStatus(Resources.Settings_ShortcutMissing, isError: true);
                    Render(values: null);
                }

                break;
        }

        return CommandResult.KeepOpen();
    }

    private void Save(string inputs)
    {
        var values = ShortcutFormTemplate.ReadValues(inputs, _shortcut.Id);

        if (ShortcutFormTemplate.Validate(values) is { } error)
        {
            // Keep the card open on what the user typed, with the error.
            _expanded = true;
            SetStatus(error, isError: true);
            Render(values);
            return;
        }

        // Collapse first: Update raises Changed synchronously, and Sync only re-renders a collapsed card.
        _expanded = false;
        _confirmingDelete = false;
        SetStatus(StringFormatter.Format(Resources.Settings_SavedTemplate, new() { ["shortcut"] = values.Name }), isError: false);

        if (!_service.Update(_shortcut.Id, values))
            SetStatus(Resources.Settings_ShortcutMissing, isError: true);

        Render(values: null);
    }

    private void SetStatus(string status, bool isError)
    {
        _status = status;
        _statusIsError = isError;
    }

    /// <param name="values">What to show in the inputs; the saved shortcut when null.</param>
    private void Render(WebSearchShortcutDataEntry? values)
    {
        var shortcut = _shortcut;

        List<string> subtitle = [];
        if (!shortcut.Enabled)
            subtitle.Add(Resources.Settings_DisabledNote);
        if (!string.IsNullOrWhiteSpace(shortcut.Keyword))
            subtitle.Add(StringFormatter.Format(Resources.Settings_KeywordTemplate, new() { ["keyword"] = shortcut.Keyword }));
        subtitle.Add(shortcut.Url);
        var summary = CardTemplates.HeaderText(shortcut.Name, string.Join("  ·  ", subtitle), dimmed: !shortcut.Enabled);

        var iconUrl = !string.IsNullOrWhiteSpace(shortcut.IconUrl) ? shortcut.IconUrl : IconService.GetFaviconUrlFromUrl(shortcut.Url);
        var switchAlt = StringFormatter.Format(shortcut.Enabled ? Resources.Settings_SwitchOnTemplate : Resources.Settings_SwitchOffTemplate, new() { ["shortcut"] = shortcut.Name });

        List<string> body =
        [
            CardTemplates.ExpanderHeader(
                iconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? iconUrl : null,
                summary,
                CardTemplates.Switch(shortcut.Enabled, ToggleEnabledAction, switchAlt),
                _expanded),
        ];

        // A status about the inputs (a validation error) goes next to them, inside the details.
        if (_status is not null && !_expanded)
            body.Add(CardTemplates.Status(_status, _statusIsError));

        List<string> fields = [.. ShortcutFormTemplate.Rows(values ?? shortcut)];

        if (_status is not null && _expanded)
            fields.Add(CardTemplates.Status(_status, _statusIsError));

        if (_confirmingDelete)
        {
            fields.Add(CardTemplates.Text(StringFormatter.Format(Resources.DeleteShortcut_ConfirmTitleTemplate, new() { ["shortcut"] = shortcut.Name }), bold: true));
            fields.Add(CardTemplates.Text(Resources.DeleteShortcut_ConfirmDescription, subtle: true));
            fields.Add(CardTemplates.ActionSet(
                CardTemplates.Submit(Resources.DeleteShortcut_ConfirmButton, ConfirmDeleteAction, validateInputs: false, style: "destructive"),
                CardTemplates.Submit(Resources.Settings_Cancel, ShortcutFormTemplate.CancelAction, validateInputs: false)));
        }
        else
        {
            fields.Add(CardTemplates.ActionSet(
                CardTemplates.Submit(Resources.AddShortcutForm_Save, ShortcutFormTemplate.SaveAction, style: "positive"),
                CardTemplates.Submit(Resources.DeleteShortcut_ConfirmButton, DeleteAction, validateInputs: false, style: "destructive")));
        }

        body.Add(CardTemplates.Details(fields, _expanded));

        SetTemplate(CardTemplates.Card(body, []));
    }

    // Re-rendering resets the card (collapses it, drops typed text), so only do it when something visible changed.
    private void SetTemplate(string json)
    {
        if (!string.Equals(TemplateJson, json, StringComparison.Ordinal))
            TemplateJson = json;
    }
}
