using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebSearchShortcut.Helpers;

internal static class AdaptiveCardHelpers
{
    /// <summary>Key in an Action.Submit's "data" naming which button was pressed.</summary>
    public const string ActionKey = "action";

    /// <summary>Serializes a string as a JSON string literal (quoted and escaped) for use in card templates.</summary>
    public static string Str(string? value) =>
        JsonSerializer.Serialize(value ?? string.Empty, AppJsonSerializerContext.Default.String);

    /// <summary>
    /// Builds Input.ChoiceSet choice objects, each prefixed with ",\n", so the result can be appended directly after
    /// a fixed first choice. An empty sequence yields an empty string, which keeps the surrounding JSON valid.
    /// </summary>
    public static string BuildAdditionalChoices(IEnumerable<(string Title, string Value)> choices)
    {
        return string.Concat(choices.Select(choice => $$"""
,
                {
                    "title": {{Str(choice.Title)}},
                    "value": {{Str(choice.Value)}}
                }
"""));
    }

    /// <summary>
    /// Returns the "action" value of the pressed Action.Submit. Hosts pass the button's data separately, but some
    /// Adaptive Card renderers merge it into the inputs, so both are checked.
    /// </summary>
    public static string? GetSubmitAction(string? inputs, string? data) =>
        ReadAction(data) ?? ReadAction(inputs);

    private static string? ReadAction(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonNode.Parse(json) is JsonObject obj
                && obj[ActionKey] is JsonValue value
                && value.TryGetValue<string>(out var action)
                    ? action
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
