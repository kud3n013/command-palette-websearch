using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.Helpers;

namespace WebSearchShortcut.Tests;

[TestClass]
public class AdaptiveCardHelpersTests
{
    // Mirrors how AddShortcutForm uses the helper: a fixed first choice followed directly by the generated ones.
    private static JsonArray BuildChoicesArray((string Title, string Value)[] choices) =>
        JsonNode.Parse($$"""[{ "title": "Default", "value": "" }{{AdaptiveCardHelpers.BuildAdditionalChoices(choices)}}]""")!.AsArray();

    [TestMethod]
    public void NoAdditionalChoicesStillProducesValidJson()
    {
        Assert.AreEqual(string.Empty, AdaptiveCardHelpers.BuildAdditionalChoices([]));
        Assert.HasCount(1, BuildChoicesArray([]));
    }

    [TestMethod]
    public void AdditionalChoicesAreAppendedAndEscaped()
    {
        var array = BuildChoicesArray([("Edge", @"C:\Program Files\msedge.exe"), ("Say \"hi\"", "x")]);

        Assert.HasCount(3, array);
        Assert.AreEqual(@"C:\Program Files\msedge.exe", array[1]!["value"]!.GetValue<string>());
        Assert.AreEqual("Say \"hi\"", array[2]!["title"]!.GetValue<string>());
    }
}
