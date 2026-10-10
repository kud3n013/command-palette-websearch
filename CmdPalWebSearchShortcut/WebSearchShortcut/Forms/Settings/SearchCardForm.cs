using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using static WebSearchShortcut.Helpers.AdaptiveCardHelpers;

namespace WebSearchShortcut;

/// <summary>
/// The heading of the shortcut list on the settings page: how many shortcuts there are and a search box that filters
/// the cards below it (by name, keyword or URL).
/// </summary>
internal sealed partial class SearchCardForm(Action<string> applyFilter) : FormContent
{
    private const string SearchAction = "search";
    private const string ClearAction = "clear";

    public override CommandResult SubmitForm(string inputs, string data)
    {
        var filter = GetSubmitAction(inputs, data) == SearchAction ? ReadQuery(inputs) : string.Empty;
        applyFilter(filter);

        return CommandResult.KeepOpen();
    }

    /// <summary>Called by the page on every rebuild.</summary>
    public void Sync(int total, int matches, string filter)
    {
        var count = total.ToString(CultureInfo.CurrentCulture);

        List<string> body =
        [
            CardTemplates.Heading(StringFormatter.Format(Resources.Settings_ListHeaderTemplate, new() { ["count"] = count })),
        ];

        if (total == 0)
        {
            body.Add(CardTemplates.Text(Resources.Settings_ListEmpty, subtle: true));
            TemplateJson = CardTemplates.Card(body, []);
            return;
        }

        body.Add($$"""
        {
            "id": "query",
            "type": "Input.Text",
            "placeholder": {{Str(Resources.Settings_SearchPlaceholder)}},
            "value": {{Str(filter)}}
        }
        """);

        if (filter.Length > 0)
        {
            body.Add(CardTemplates.Text(StringFormatter.Format(
                matches == 0 ? Resources.Settings_NoMatchesTemplate : Resources.Settings_MatchesTemplate,
                new() { ["matches"] = matches.ToString(CultureInfo.CurrentCulture), ["count"] = count, ["query"] = filter }), subtle: true));
        }

        // Search comes first: newer Command Palette versions run a card's first action when Enter is pressed in a text box.
        List<string> actions = [CardTemplates.Submit(Resources.Settings_SearchButton, SearchAction)];
        if (filter.Length > 0)
            actions.Add(CardTemplates.Submit(Resources.Settings_ClearSearch, ClearAction, validateInputs: false));

        TemplateJson = CardTemplates.Card(body, actions);
    }

    private static string ReadQuery(string inputs)
    {
        try
        {
            return JsonNode.Parse(inputs) is JsonObject root && root["query"] is JsonValue value && value.TryGetValue<string>(out var query)
                ? query.Trim()
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
