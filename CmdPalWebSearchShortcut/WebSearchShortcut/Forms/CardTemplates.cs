using System.Collections.Generic;
using WebSearchShortcut.Helpers;
using static WebSearchShortcut.Helpers.AdaptiveCardHelpers;

namespace WebSearchShortcut;

/// <summary>
/// Small builders for Adaptive Card JSON. Every value goes through <see cref="Str"/>, so user text can't break the JSON.
/// </summary>
internal static class CardTemplates
{
    public static string Card(IEnumerable<string> body, IEnumerable<string> actions) => $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {{string.Join(",\n", body)}}
    ],
    "actions": [
        {{string.Join(",\n", actions)}}
    ]
}
""";

    public static string Heading(string text) => $$"""
{ "type": "TextBlock", "text": {{Str(text)}}, "weight": "Bolder", "size": "Medium", "wrap": true }
""";

    public static string Text(string text, bool subtle = false, string color = "Default", bool bold = false) => $$"""
{ "type": "TextBlock", "text": {{Str(text)}}, "wrap": true, "isSubtle": {{(subtle ? "true" : "false")}}, "color": {{Str(color)}}, "weight": {{Str(bold ? "Bolder" : "Default")}} }
""";

    /// <summary>An inline result message: green for success, red for errors.</summary>
    public static string Status(string text, bool isError) => Text(text, color: isError ? "Attention" : "Good");

    /// <summary>
    /// A submit button whose data carries <c>{"action": action}</c>. With <paramref name="validateInputs"/> false
    /// (e.g. Cancel) required inputs aren't validated or sent.
    /// </summary>
    public static string Submit(string title, string action, bool validateInputs = true, string style = "default") => $$"""
{
    "type": "Action.Submit",
    "title": {{Str(title)}},
    "style": {{Str(style)}},
    "associatedInputs": {{Str(validateInputs ? "auto" : "none")}},
    "data": { {{Str(ActionKey)}}: {{Str(action)}} }
}
""";

    public static string ActionSet(params string[] actions) => $$"""
{ "type": "ActionSet", "actions": [ {{string.Join(",\n", actions)}} ] }
""";

    // Expandable cards: the header's click area shows/hides the "details" container and swaps the chevrons. This runs in
    // the card renderer, so expanding is instant and the extension isn't told; a re-rendered card starts collapsed
    // unless it is rendered expanded on purpose (e.g. to show a validation error).
    private const string DetailsId = "details";

    private static string ToggleDetails() => $$"""
{ "type": "Action.ToggleVisibility", "targetElements": [ {{Str(DetailsId)}}, "chevronDown", "chevronUp" ] }
""";

    /// <summary>
    /// The always-visible row of an expandable card: an optional icon, the text column, an optional extra column
    /// that is not part of the click area (e.g. a switch), and the chevron.
    /// </summary>
    public static string ExpanderHeader(string? iconUrl, IEnumerable<string> textItems, string? extraColumn, bool expanded)
    {
        List<string> columns = [];

        if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            columns.Add($$"""
{
    "type": "Column", "width": "auto", "verticalContentAlignment": "Center", "selectAction": {{ToggleDetails()}},
    "items": [ { "type": "Image", "url": {{Str(iconUrl)}}, "width": "32px", "height": "32px" } ]
}
""");
        }

        columns.Add($$"""
{
    "type": "Column", "width": "stretch", "verticalContentAlignment": "Center", "selectAction": {{ToggleDetails()}},
    "items": [ {{string.Join(",\n", textItems)}} ]
}
""");

        if (extraColumn is not null)
            columns.Add(extraColumn);

        columns.Add($$"""
{
    "type": "Column", "width": "auto", "verticalContentAlignment": "Center", "selectAction": {{ToggleDetails()}},
    "items": [
        { "type": "Image", "id": "chevronDown", "url": {{Str(CardImages.ChevronDown)}}, "width": "16px", "height": "16px", "isVisible": {{Bool(!expanded)}} },
        { "type": "Image", "id": "chevronUp", "url": {{Str(CardImages.ChevronUp)}}, "width": "16px", "height": "16px", "isVisible": {{Bool(expanded)}} }
    ]
}
""");

        return $$"""
{ "type": "ColumnSet", "columns": [ {{string.Join(",\n", columns)}} ] }
""";
    }

    /// <summary>The title and subtitle of an expander header, styled like Command Palette's settings.</summary>
    public static IEnumerable<string> HeaderText(string title, string? subtitle, bool dimmed = false)
    {
        yield return Text(title, subtle: dimmed);
        if (!string.IsNullOrWhiteSpace(subtitle))
            yield return $$"""
{ "type": "TextBlock", "text": {{Str(subtitle)}}, "wrap": true, "isSubtle": true, "size": "Small", "spacing": "None" }
""";
    }

    /// <summary>
    /// A settings row: name and description on the left, <paramref name="input"/> on the right, with a separator above,
    /// like the rows inside Command Palette's own settings expanders.
    /// </summary>
    public static string SettingRow(string label, string description, string input) => $$"""
{
    "type": "ColumnSet", "separator": true, "spacing": "Medium",
    "columns": [
        {
            "type": "Column", "width": 3, "verticalContentAlignment": "Center",
            "items": [
                { "type": "TextBlock", "text": {{Str(label)}}, "wrap": true },
                { "type": "TextBlock", "text": {{Str(description)}}, "wrap": true, "isSubtle": true, "size": "Small", "spacing": "None" }
            ]
        },
        { "type": "Column", "width": 2, "verticalContentAlignment": "Center", "items": [ {{input}} ] }
    ]
}
""";

    /// <summary>A checkbox row: the Input.Toggle (its title is the label) with the description under it.</summary>
    public static string CheckboxRow(string toggleInput, string description) => $$"""
{
    "type": "Container", "separator": true, "spacing": "Medium",
    "items": [
        {{toggleInput}},
        {
            "type": "ColumnSet", "spacing": "None",
            "columns": [
                { "type": "Column", "width": "28px" },
                { "type": "Column", "width": "stretch", "items": [ { "type": "TextBlock", "text": {{Str(description)}}, "wrap": true, "isSubtle": true, "size": "Small" } ] }
            ]
        }
    ]
}
""";

    /// <summary>The part of an expandable card that the header shows and hides.</summary>
    public static string Details(IEnumerable<string> items, bool expanded) => $$"""
{
    "type": "Container", "id": {{Str(DetailsId)}}, "isVisible": {{Bool(expanded)}}, "separator": true, "spacing": "Medium",
    "items": [ {{string.Join(",\n", items)}} ]
}
""";

    /// <summary>An on/off switch column that submits <paramref name="action"/> when clicked, without validating inputs.</summary>
    public static string Switch(bool on, string action, string altText) => $$"""
{
    "type": "Column", "width": "auto", "verticalContentAlignment": "Center",
    "selectAction": {{Submit(altText, action, validateInputs: false)}},
    "items": [ { "type": "Image", "url": {{Str(on ? CardImages.SwitchOn : CardImages.SwitchOff)}}, "width": "40px", "height": "20px", "altText": {{Str(altText)}} } ]
}
""";

    private static string Bool(bool value) => value ? "true" : "false";
}
