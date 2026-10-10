using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;

namespace WebSearchShortcut;

/// <summary>
/// First card of the settings page: a one-line "Add" header that expands to the shortcut form.
/// State changes swap this card's template only, so other cards keep what the user typed.
/// </summary>
internal sealed partial class AddShortcutCardForm : FormContent
{
    private readonly ShortcutService _service;

    public AddShortcutCardForm(ShortcutService service)
    {
        _service = service;
        Render(values: null, status: null, isError: false);
    }

    public override CommandResult SubmitForm(string inputs, string data)
    {
        if (AdaptiveCardHelpers.GetSubmitAction(inputs, data) != ShortcutFormTemplate.SaveAction)
            return CommandResult.KeepOpen();

        var values = ShortcutFormTemplate.ReadValues(inputs, string.Empty);

        if (ShortcutFormTemplate.Validate(values) is { } error)
            Render(values, error, isError: true);
        else if (_service.Add(values))
            Render(values: null, StringFormatter.Format(Resources.Settings_AddedTemplate, new() { ["shortcut"] = values.Name }), isError: false);
        else
            Render(values, Resources.StorageLoadError_AddBlocked, isError: true);

        return CommandResult.KeepOpen();
    }

    /// <param name="values">Inputs to show again after an error; the card renders expanded when there is an error.</param>
    private void Render(WebSearchShortcutDataEntry? values, string? status, bool isError)
    {
        List<string> body =
        [
            CardTemplates.ExpanderHeader(
                CardImages.Add,
                CardTemplates.HeaderText(Resources.AddShortcutItem_Title, Resources.Settings_AddDescription),
                extraColumn: null,
                expanded: isError),
        ];

        // A success message belongs to the collapsed card; an error to the form it is about.
        if (status is not null && !isError)
            body.Add(CardTemplates.Status(status, isError: false));

        List<string> fields = [.. ShortcutFormTemplate.Rows(values)];
        if (status is not null && isError)
            fields.Add(CardTemplates.Status(status, isError: true));
        fields.Add(CardTemplates.ActionSet(CardTemplates.Submit(Resources.Settings_AddButton, ShortcutFormTemplate.SaveAction, style: "positive")));

        body.Add(CardTemplates.Details(fields, expanded: isError));

        TemplateJson = CardTemplates.Card(body, []);
    }
}
