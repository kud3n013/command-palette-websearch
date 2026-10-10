using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using static WebSearchShortcut.Helpers.AdaptiveCardHelpers;

namespace WebSearchShortcut;

/// <summary>
/// Settings page card for batch import (pasted JSON or a .json file path) and export to the clipboard.
/// Collapsed until opened; the result is shown inside the card, which stays open once it has been used.
/// </summary>
internal sealed partial class ImportExportForm : FormContent
{
    private const string ImportAction = "import";
    private const string ExportAction = "export";

    private readonly ShortcutService _service;

    // Kept so a failed import doesn't wipe what the user pasted.
    private string _text = string.Empty;
    private bool _replaceExisting;
    private string? _status;
    private bool _statusIsError;
    private bool _expanded;

    public ImportExportForm(ShortcutService service)
    {
        _service = service;
        Render();
    }

    public override CommandResult SubmitForm(string inputs, string data)
    {
        ReadInputs(inputs);
        _expanded = true;

        switch (GetSubmitAction(inputs, data))
        {
            case ImportAction:
                Import();
                break;

            case ExportAction:
                Export();
                break;
        }

        Render();

        return CommandResult.KeepOpen();
    }

    private void Import()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            SetStatus(Resources.Import_EmptyInput, isError: true);
            return;
        }

        string json = _text;
        if (ShortcutImporter.TryGetFilePath(_text, out var path))
        {
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                SetStatus(StringFormatter.Format(Resources.Import_FileErrorTemplate, new() { ["path"] = path, ["error"] = ex.Message }), isError: true);
                return;
            }
        }

        ImportParseResult parsed;
        try
        {
            parsed = ShortcutImporter.Parse(json);
        }
        catch (JsonException ex)
        {
            SetStatus(StringFormatter.Format(Resources.Import_InvalidJsonTemplate, new() { ["error"] = ex.Message }), isError: true);
            return;
        }

        if (parsed.Entries.Count == 0)
        {
            SetStatus(StringFormatter.Format(Resources.Import_NothingFoundTemplate, new() { ["invalid"] = parsed.InvalidCount.ToString(System.Globalization.CultureInfo.CurrentCulture) }), isError: true);
            return;
        }

        // Clear the input before importing: the import raises Changed, and the page rebuild re-reads this card.
        var previousText = _text;
        _text = string.Empty;

        var result = _service.Import(parsed.Entries, _replaceExisting);
        if (result is null)
        {
            _text = previousText;
            SetStatus(Resources.StorageLoadError_AddBlocked, isError: true);
            return;
        }

        SetStatus(StringFormatter.Format(Resources.Import_ResultTemplate, new()
        {
            ["added"] = Count(result.Added),
            ["updated"] = Count(result.Updated),
            ["skipped"] = Count(result.Skipped),
            ["invalid"] = Count(parsed.InvalidCount),
        }), isError: false);
    }

    private void Export()
    {
        var count = _service.GetShortcuts().Count;

        // ClipboardHelper marshals to an STA thread itself, so this works from the host's background thread.
        ClipboardHelper.SetText(_service.ExportJson());

        SetStatus(StringFormatter.Format(Resources.Export_ResultTemplate, new() { ["count"] = Count(count) }), isError: false);
    }

    private static string Count(int n) => n.ToString(System.Globalization.CultureInfo.CurrentCulture);

    private void SetStatus(string status, bool isError)
    {
        _status = status;
        _statusIsError = isError;
    }

    private void ReadInputs(string inputs)
    {
        try
        {
            if (JsonNode.Parse(inputs) is not JsonObject root)
                return;

            if (root["importText"] is JsonValue text && text.TryGetValue<string>(out var t))
                _text = t;

            if (root["replaceExisting"] is JsonValue replace && replace.TryGetValue<string>(out var r))
                _replaceExisting = string.Equals(r, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
        }
    }

    private void Render()
    {
        var fields = new System.Collections.Generic.List<string>
        {
            CardTemplates.Text(Resources.Import_Description, subtle: true),
            $$"""
            {
                "id": "importText",
                "type": "Input.Text",
                "isMultiline": true,
                "placeholder": {{Str(Resources.Import_Placeholder)}},
                "value": {{Str(_text)}}
            }
            """,
            $$"""
            {
                "id": "replaceExisting",
                "type": "Input.Toggle",
                "title": {{Str(Resources.Import_ReplaceToggle)}},
                "valueOn": "true",
                "valueOff": "false",
                "value": {{Str(_replaceExisting ? "true" : "false")}}
            }
            """,
        };

        if (_status is not null)
            fields.Add(CardTemplates.Status(_status, _statusIsError));

        fields.Add(CardTemplates.ActionSet(
            CardTemplates.Submit(Resources.Import_Button, ImportAction, style: "positive"),
            CardTemplates.Submit(Resources.Export_Button, ExportAction)));

        TemplateJson = CardTemplates.Card(
        [
            CardTemplates.ExpanderHeader(CardImages.Import, CardTemplates.HeaderText(Resources.Import_Header, Resources.Import_Summary), extraColumn: null, _expanded),
            CardTemplates.Details(fields, _expanded),
        ], []);
    }
}
