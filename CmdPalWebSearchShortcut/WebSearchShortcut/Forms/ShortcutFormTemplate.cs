using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebSearchShortcut.Browser;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using static WebSearchShortcut.Helpers.AdaptiveCardHelpers;

namespace WebSearchShortcut;

/// <summary>
/// The shortcut input fields, shared by the palette's Add/Edit page and the settings page cards.
/// </summary>
internal static class ShortcutFormTemplate
{
    public const string SaveAction = "save";
    public const string CancelAction = "cancel";

    /// <summary>One form field: the input (without a label) and how to present it.</summary>
    /// <param name="ShortPlaceholder">Placeholder for the row layout, where <paramref name="Description"/> is shown
    /// next to the input; null keeps the input's own placeholder.</param>
    private sealed record Field(string Id, string Label, string Description, string Input, string? ShortPlaceholder = null, bool IsCheckbox = false);

    /// <summary>Stacked inputs with labels above them, for the palette's Add/Edit page.</summary>
    public static IEnumerable<string> Inputs(WebSearchShortcutDataEntry? shortcut) =>
        Fields(shortcut).Select(f => f.IsCheckbox ? f.Input : WithProperty(f.Input, "label", f.Label));

    /// <summary>
    /// Settings-style rows for the settings page cards: name and description on the left, the input on the right,
    /// like Command Palette's own settings. Checkboxes span the row with the description under them.
    /// </summary>
    public static IEnumerable<string> Rows(WebSearchShortcutDataEntry? shortcut) =>
        Fields(shortcut).Select(f => f.IsCheckbox
            ? CardTemplates.CheckboxRow(f.Input, f.Description)
            : CardTemplates.SettingRow(f.Label, f.Description, f.ShortPlaceholder is null ? f.Input : WithProperty(f.Input, "placeholder", f.ShortPlaceholder)));

    private static IEnumerable<Field> Fields(WebSearchShortcutDataEntry? shortcut)
    {
        var browserPath = shortcut?.BrowserPath ?? string.Empty;

        yield return new("name", Resources.AddShortcutForm_Name_Label, Resources.AddShortcutForm_Name_Description, $$"""
        {
            "id": "name",
            "type": "Input.Text",
            "value": {{Str(shortcut?.Name)}},
            "isRequired": true,
            "errorMessage": {{Str(Resources.AddShortcutForm_Name_ErrorMessage)}}
        }
""");
        yield return new("url", Resources.AddShortcutForm_Url_Label, Resources.AddShortcutForm_Url_Placeholder, $$"""
        {
            "id": "url",
            "type": "Input.Text",
            "style": "Url",
            "placeholder": {{Str(Resources.AddShortcutForm_Url_Placeholder)}},
            "value": {{Str(shortcut?.Url)}},
            "isRequired": true,
            "errorMessage": {{Str(Resources.AddShortcutForm_Url_ErrorMessage)}}
        }
""", ShortPlaceholder: "https://www.example.com/search?q=%s");
        yield return new("keyword", Resources.AddShortcutForm_Keyword_Label, Resources.AddShortcutForm_Keyword_Placeholder, $$"""
        {
            "id": "keyword",
            "type": "Input.Text",
            "placeholder": {{Str(Resources.AddShortcutForm_Keyword_Placeholder)}},
            "value": {{Str(shortcut?.Keyword)}},
            "regex": "^\\S*$",
            "errorMessage": {{Str(Resources.AddShortcutForm_Keyword_ErrorMessage)}}
        }
""", ShortPlaceholder: Resources.AddShortcutForm_Keyword_ShortPlaceholder);
        yield return new("useAsFallback", Resources.AddShortcutForm_UseAsFallback_Label, Resources.AddShortcutForm_UseAsFallback_Title, $$"""
        {
            "id": "useAsFallback",
            "type": "Input.Toggle",
            "title": {{Str(Resources.AddShortcutForm_UseAsFallback_Label)}},
            "valueOn": "true",
            "valueOff": "false",
            "value": {{Str(shortcut?.UseAsFallback == false ? "false" : "true")}}
        }
""", IsCheckbox: true);
        yield return new("suggestionProvider", Resources.AddShortcutForm_SuggestionProvider_Label, Resources.AddShortcutForm_SuggestionProvider_Description, $$"""
        {
            "id": "suggestionProvider",
            "type": "Input.ChoiceSet",
            "placeholder": {{Str(Resources.AddShortcutForm_SuggestionProvider_Placeholder)}},
            "choices": [
                {
                    "title": {{Str(Resources.AddShortcutForm_SuggestionProvider_None)}},
                    "value": ""
                }{{BuildAdditionalChoices(SuggestionsRegistry.ProviderNames.Select(key => (key, key)))}}
            ],
            "value": {{Str(shortcut?.SuggestionProvider)}},
            "errorMessage": "// Just for space between items"
        }
""");
        yield return new("homePage", Resources.AddShortcutForm_Homepage_Label, Resources.AddShortcutForm_Homepage_Description, $$"""
        {
            "id": "homePage",
            "type": "Input.Text",
            "style": "Url",
            "placeholder": {{Str(Resources.AddShortcutForm_Homepage_Placeholder)}},
            "value": {{Str(shortcut?.HomePage)}},
            "errorMessage": "// Just for space between items"
        }
""");
        yield return new("replaceWhitespace", Resources.AddShortcutForm_ReplaceWhitespace_Label, Resources.AddShortcutForm_ReplaceWhitespace_Description, $$"""
        {
            "id": "replaceWhitespace",
            "type": "Input.Text",
            "placeholder": {{Str(Resources.AddShortcutForm_ReplaceWhitespace_Placeholder)}},
            "value": {{Str(shortcut?.ReplaceWhitespace)}},
            "errorMessage": "// Just for space between items"
        }
""", ShortPlaceholder: "+");
        yield return new("browserPath", Resources.AddShortcutForm_BrowserPath_Label, Resources.AddShortcutForm_BrowserPath_Description, $$"""
        {
            "id": "browserPath",
            "type": "Input.ChoiceSet",
            "placeholder": {{Str(browserPath)}},
            "choices": [
                {
                    "title": {{Str(Resources.AddShortcutForm_BrowserPath_Default)}},
                    "value": ""
                }{{BuildAdditionalChoices(BrowsersDiscovery.GetInstalledBrowsers()
                    .Where(b => !string.IsNullOrWhiteSpace(b.Path))
                    .Select(b => (b.Name, b.Path)))}}
            ],
            "value": {{Str(browserPath)}},
            "errorMessage": "// Just for space between items"
        }
""");
        yield return new("browserArgs", Resources.AddShortcutForm_BrowserArgs_Label, Resources.AddShortcutForm_BrowserArgs_Placeholder, $$"""
        {
            "id": "browserArgs",
            "type": "Input.Text",
            "placeholder": {{Str(Resources.AddShortcutForm_BrowserArgs_Placeholder)}},
            "value": {{Str(shortcut?.BrowserArgs)}},
            "errorMessage": "// Just for space between items"
        }
""", ShortPlaceholder: "%1");
    }

    /// <summary>Adds (or, for "placeholder", replaces) a top-level string property of an input's JSON.</summary>
    private static string WithProperty(string inputJson, string name, string value)
    {
        var node = JsonNode.Parse(inputJson)!.AsObject();
        node[name] = value;

        return node.ToJsonString(AppJsonSerializerContext.Default.Options);
    }

    /// <summary>Reads the submitted inputs into a new entry; the caller decides whether it's an add or an edit.</summary>
    public static WebSearchShortcutDataEntry ReadValues(string inputs, string id)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(inputs) as JsonObject;
        }
        catch (JsonException)
        {
            root = null;
        }

        string? Get(string key, bool trim = true)
        {
            var value = root?[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
            value = trim ? value?.Trim() : value;

            return string.IsNullOrEmpty(value) ? null : value;
        }

        return new WebSearchShortcutDataEntry
        {
            Id = id,
            Name = Get("name") ?? string.Empty,
            Url = Get("url") ?? string.Empty,
            Keyword = Get("keyword"),
            SuggestionProvider = Get("suggestionProvider"),
            HomePage = Get("homePage"),
            // Whitespace is meaningful here (e.g. "%20" vs " "), so don't trim it.
            ReplaceWhitespace = Get("replaceWhitespace", trim: false),
            BrowserPath = Get("browserPath"),
            BrowserArgs = Get("browserArgs"),
            // Absent (a form without the toggle) keeps the default, on.
            UseAsFallback = !string.Equals(Get("useAsFallback"), "false", System.StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>Server-side checks, since not every host enforces the card's own validation.</summary>
    /// <returns>A localized error message, or null when the values are valid.</returns>
    public static string? Validate(WebSearchShortcutDataEntry values)
    {
        if (string.IsNullOrWhiteSpace(values.Name))
            return Resources.AddShortcutForm_Name_ErrorMessage;

        if (string.IsNullOrWhiteSpace(values.Url))
            return Resources.AddShortcutForm_Url_ErrorMessage;

        if (!WebSearchShortcutDataEntry.IsValidKeyword(values.Keyword))
            return Resources.AddShortcutForm_Keyword_ErrorMessage;

        return null;
    }
}
