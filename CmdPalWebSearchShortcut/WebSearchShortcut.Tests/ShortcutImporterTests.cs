using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.Services;

namespace WebSearchShortcut.Tests;

[TestClass]
public class ShortcutImporterTests
{
    private static string Names(IEnumerable<WebSearchShortcutDataEntry> entries) => string.Join("|", entries.Select(e => e.Name));

    [TestMethod]
    public void ParsesShortcutsFileFormat()
    {
        var result = ShortcutImporter.Parse("""
            { "Data": [
                { "Id": "WebSearchShortcut1", "Name": "Google", "Url": "https://www.google.com/search?q=%s", "Keyword": "g", "SuggestionProvider": "Google" },
                { "Name": "Wiki", "Url": "https://fr.wikipedia.org/w/index.php?search=%s", "ReplaceWhitespace": "%20", "HomePage": "https://fr.wikipedia.org", "BrowserPath": "C:\\b.exe", "BrowserArgs": "--private %1", "IconUrl": "https://x/icon.png" }
            ] }
            """);

        Assert.AreEqual(0, result.InvalidCount);
        Assert.AreEqual("Google|Wiki", Names(result.Entries));
        Assert.AreEqual("WebSearchShortcut1", result.Entries[0].Id);
        Assert.AreEqual("g", result.Entries[0].Keyword);
        Assert.AreEqual("Google", result.Entries[0].SuggestionProvider);

        var wiki = result.Entries[1];
        Assert.AreEqual("%20", wiki.ReplaceWhitespace);
        Assert.AreEqual("https://fr.wikipedia.org", wiki.HomePage);
        Assert.AreEqual(@"C:\b.exe", wiki.BrowserPath);
        Assert.AreEqual("--private %1", wiki.BrowserArgs);
        Assert.AreEqual("https://x/icon.png", wiki.IconUrl);
    }

    [TestMethod]
    public void ParsesArrayAndIsCaseInsensitive()
    {
        var result = ShortcutImporter.Parse("""[ { "name": "A", "url": "https://a/%s" }, { "NAME": "B", "URL": "https://b/%s" } ]""");

        Assert.AreEqual("A|B", Names(result.Entries));
        Assert.AreEqual("https://b/%s", result.Entries[1].Url);
    }

    [TestMethod]
    public void ParsesSingleShortcut()
    {
        var result = ShortcutImporter.Parse("""{ "Name": "A", "Url": "https://a/%s" }""");

        Assert.AreEqual("A", Names(result.Entries));
    }

    [TestMethod]
    public void ParsesPowerToysRunFormat()
    {
        var result = ShortcutImporter.Parse("""
            {
              "Google": { "Url": "https://www.google.com/search?q=%s", "SuggestionProvider": "google", "IsDefault": true },
              "GitHubStars": { "Url": "https://github.com/stars?q=%s", "Keyword": "gs" },
              "Multi": { "Urls": [ "", "https://first/%s", "https://second/%s" ] },
              "Data": { "Url": "https://named-data/%s" },
              "Url": { "Url": "https://named-url/%s" }
            }
            """);

        Assert.AreEqual(0, result.InvalidCount);
        Assert.AreEqual("Google|GitHubStars|Multi|Data|Url", Names(result.Entries));
        Assert.AreEqual("Google", result.Entries[0].SuggestionProvider, "provider names are normalized");
        Assert.AreEqual("gs", result.Entries[1].Keyword);
        Assert.AreEqual("https://first/%s", result.Entries[2].Url, "first non-empty Urls entry");
        Assert.AreEqual("https://named-data/%s", result.Entries[3].Url);
    }

    [TestMethod]
    public void AllowsCommentsAndTrailingCommas()
    {
        var result = ShortcutImporter.Parse("""
            [
              // my engines
              { "Name": "A", "Url": "https://a/%s", },
            ]
            """);

        Assert.AreEqual("A", Names(result.Entries));
    }

    [TestMethod]
    public void CountsInvalidEntries()
    {
        var result = ShortcutImporter.Parse("""
            [
              { "Name": "Ok", "Url": "https://ok/%s" },
              { "Name": "No url" },
              { "Url": "https://no-name/%s" },
              { "Name": "Bad keyword", "Url": "https://x/%s", "Keyword": "two words" },
              { "Name": "  ", "Url": "https://blank-name/%s" },
              "not an object",
              42
            ]
            """);

        Assert.AreEqual("Ok", Names(result.Entries));
        Assert.AreEqual(6, result.InvalidCount);
    }

    [TestMethod]
    public void TrimsValuesButKeepsReplaceWhitespaceAsIs()
    {
        var entry = ShortcutImporter.Parse("""{ "Name": "  A  ", "Url": " https://a/%s ", "ReplaceWhitespace": " " }""").Entries[0];

        Assert.AreEqual("A", entry.Name);
        Assert.AreEqual("https://a/%s", entry.Url);
        Assert.AreEqual(" ", entry.ReplaceWhitespace);
    }

    [TestMethod]
    public void DropsUnknownSuggestionProviders()
    {
        var entry = ShortcutImporter.Parse("""{ "Name": "A", "Url": "https://a/%s", "SuggestionProvider": "Yandex" }""").Entries[0];

        Assert.IsNull(entry.SuggestionProvider);
    }

    [TestMethod]
    public void IgnoresNonStringValues()
    {
        var entry = ShortcutImporter.Parse("""{ "Name": "A", "Url": "https://a/%s", "Keyword": 5, "HomePage": null }""").Entries[0];

        Assert.IsNull(entry.Keyword);
        Assert.IsNull(entry.HomePage);
    }

    [TestMethod]
    public void UnsupportedShapesYieldNothing()
    {
        Assert.IsEmpty(ShortcutImporter.Parse("42").Entries);
        Assert.IsEmpty(ShortcutImporter.Parse("{}").Entries);
        Assert.IsEmpty(ShortcutImporter.Parse("[]").Entries);
    }

    [TestMethod]
    public void InvalidJsonThrows()
    {
        Assert.Throws<JsonException>(() => ShortcutImporter.Parse("{ not json"));
    }

    [TestMethod]
    public void MergeSkipsExistingNamesByDefault()
    {
        List<WebSearchShortcutDataEntry> target = [new() { Id = "id-google", Name = "Google", Url = "https://old/%s" }];

        var result = ShortcutImporter.Merge(target,
        [
            new() { Name = "google", Url = "https://new/%s" },
            new() { Name = "Bing", Url = "https://bing/%s" },
        ], replaceExisting: false);

        Assert.AreEqual((1, 0, 1), (result.Added, result.Updated, result.Skipped));
        Assert.AreEqual("Google|Bing", Names(target));
        Assert.AreEqual("https://old/%s", target[0].Url);
        Assert.AreEqual("Bing", Names(result.Changed));
    }

    [TestMethod]
    public void MergeReplaceKeepsTheExistingId()
    {
        var existing = new WebSearchShortcutDataEntry { Id = "id-google", Name = "Google", Url = "https://old/%s", Keyword = "g" };
        List<WebSearchShortcutDataEntry> target = [existing];

        var result = ShortcutImporter.Merge(target, [new() { Id = "other-id", Name = "GOOGLE", Url = "https://new/%s" }], replaceExisting: true);

        Assert.AreEqual((0, 1, 0), (result.Added, result.Updated, result.Skipped));
        Assert.HasCount(1, target);
        Assert.AreSame(existing, target[0]);
        Assert.AreEqual("id-google", existing.Id);
        Assert.AreEqual("GOOGLE", existing.Name);
        Assert.AreEqual("https://new/%s", existing.Url);
        Assert.IsNull(existing.Keyword, "replace copies every value, including cleared ones");
    }

    [TestMethod]
    public void MergeKeepsImportedIdsUnlessTheyClash()
    {
        List<WebSearchShortcutDataEntry> target = [new() { Id = "taken", Name = "Existing", Url = "https://e/%s" }];

        ShortcutImporter.Merge(target,
        [
            new() { Id = "taken", Name = "Clash", Url = "https://c/%s" },
            new() { Id = "fresh", Name = "Fresh", Url = "https://f/%s" },
        ], replaceExisting: false);

        Assert.AreEqual(string.Empty, target[1].Id, "clashing Id is cleared so Storage assigns a new one");
        Assert.AreEqual("fresh", target[2].Id);
    }

    [TestMethod]
    public void MergeHandlesDuplicatesWithinTheImport()
    {
        List<WebSearchShortcutDataEntry> target = [];

        var result = ShortcutImporter.Merge(target,
        [
            new() { Name = "A", Url = "https://first/%s" },
            new() { Name = "a", Url = "https://second/%s" },
        ], replaceExisting: false);

        Assert.AreEqual((1, 0, 1), (result.Added, result.Updated, result.Skipped));
        Assert.AreEqual("https://first/%s", target[0].Url);
    }

    [TestMethod]
    public void ExportRoundTripsThroughImport()
    {
        var storage = new Storage();
        storage.Data.Add(new WebSearchShortcutDataEntry { Name = "A", Url = "https://a/%s", Keyword = "a", SuggestionProvider = "Bing" });
        storage.Data.Add(new WebSearchShortcutDataEntry { Name = "B", Url = "https://b/%s", ReplaceWhitespace = "%20" });

        var parsed = ShortcutImporter.Parse(Storage.ToJson(storage));

        Assert.AreEqual("A|B", Names(parsed.Entries));
        Assert.AreEqual(storage.Data[0].Id, parsed.Entries[0].Id);
        Assert.AreEqual("a", parsed.Entries[0].Keyword);
        Assert.AreEqual("Bing", parsed.Entries[0].SuggestionProvider);
        Assert.AreEqual("%20", parsed.Entries[1].ReplaceWhitespace);
    }

    [TestMethod]
    [DataRow("C:\\shortcuts.json", "C:\\shortcuts.json")]
    [DataRow("  \"C:\\My Files\\shortcuts.json\"  ", "C:\\My Files\\shortcuts.json")]
    [DataRow("shortcuts.json", "shortcuts.json")]
    public void FilePathInputIsRecognized(string input, string expected)
    {
        Assert.IsTrue(ShortcutImporter.TryGetFilePath(input, out var path));
        Assert.AreEqual(expected, path);
    }

    [TestMethod]
    [DataRow("{ \"Data\": [] }")]
    [DataRow("  [ ]")]
    [DataRow("   ")]
    [DataRow("\"\"")]
    public void JsonOrEmptyInputIsNotAPath(string input)
    {
        Assert.IsFalse(ShortcutImporter.TryGetFilePath(input, out _));
    }
}
