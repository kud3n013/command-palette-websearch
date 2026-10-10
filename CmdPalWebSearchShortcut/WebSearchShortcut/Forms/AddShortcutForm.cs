using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;

namespace WebSearchShortcut;

/// <summary>
/// Add/Edit form opened from the palette. Saves through <see cref="ShortcutService"/>, which owns the shortcuts list.
/// </summary>
internal sealed partial class AddShortcutForm : FormContent
{
    private readonly WebSearchShortcutDataEntry? _shortcut;
    private readonly ShortcutService _service;

    public AddShortcutForm(WebSearchShortcutDataEntry? shortcut, ShortcutService service)
    {
        _shortcut = shortcut;
        _service = service;

        TemplateJson = CardTemplates.Card(
            ShortcutFormTemplate.Inputs(shortcut),
            [CardTemplates.Submit(Resources.AddShortcutForm_Save, ShortcutFormTemplate.SaveAction)]);
    }

    public override CommandResult SubmitForm(string inputs, string data)
    {
        var values = ShortcutFormTemplate.ReadValues(inputs, _shortcut?.Id ?? string.Empty);

        if (ShortcutFormTemplate.Validate(values) is { } error)
            return CommandResult.ShowToast(error);

        bool saved = _shortcut is null
            ? _service.Add(values)
            : _service.Update(_shortcut.Id, values);

        return saved
            ? CommandResult.GoHome()
            : CommandResult.ShowToast(Resources.StorageLoadError_AddBlocked);
    }
}
