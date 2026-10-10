using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.Services;

namespace WebSearchShortcut.Tests;

/// <summary>Enabled / UseAsFallback (the settings page switches) and the settings page search.</summary>
[TestClass]
public class ShortcutSwitchesTests
{
    [TestMethod]
    public void FilesWithoutTheSwitchesLoadWithBothOn()
    {
        var directory = Path.Combine(Path.GetTempPath(), "WebSearchShortcutTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "WebSearchShortcut.json");
            File.WriteAllText(path, """{ "Data": [ { "Id": "1", "Name": "Google", "Url": "https://www.google.com/search?q=%s" } ] }""");

            var entry = Storage.ReadFromFile(path).Data[0];

            Assert.IsTrue(entry.Enabled);
            Assert.IsTrue(entry.UseAsFallback);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void SwitchesRoundTrip()
    {
        var storage = new Storage();
        storage.Data.Add(new WebSearchShortcutDataEntry { Name = "Off", Url = "https://off/%s", Enabled = false, UseAsFallback = false });

        var parsed = ShortcutImporter.Parse(Storage.ToJson(storage));

        Assert.IsFalse(parsed.Entries[0].Enabled);
        Assert.IsFalse(parsed.Entries[0].UseAsFallback);
    }

    [TestMethod]
    public void ImportReadsSwitchesAndDefaultsToOn()
    {
        var parsed = ShortcutImporter.Parse("""
        [
            { "name": "A", "url": "https://a/%s", "enabled": false, "useAsFallback": "false" },
            { "name": "B", "url": "https://b/%s" }
        ]
        """);

        Assert.IsFalse(parsed.Entries[0].Enabled);
        Assert.IsFalse(parsed.Entries[0].UseAsFallback);
        Assert.IsTrue(parsed.Entries[1].Enabled);
        Assert.IsTrue(parsed.Entries[1].UseAsFallback);
    }

    [TestMethod]
    public void CopyValuesFromCopiesSwitches()
    {
        var target = new WebSearchShortcutDataEntry { Id = "t", Name = "T", Url = "https://t/%s" };

        target.CopyValuesFrom(new WebSearchShortcutDataEntry { Name = "S", Url = "https://s/%s", Enabled = false, UseAsFallback = false });

        Assert.IsFalse(target.Enabled);
        Assert.IsFalse(target.UseAsFallback);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow("  ", true)]
    [DataRow("goo", true)]   // name
    [DataRow("GOOGLE", true)] // case-insensitive
    [DataRow("g", true)]     // keyword (and name)
    [DataRow("search?q", true)] // URL
    [DataRow(" goo ", true)] // trimmed
    [DataRow("bing", false)]
    public void MatchesNameKeywordOrUrl(string filter, bool expected)
    {
        var entry = new WebSearchShortcutDataEntry { Name = "Google", Keyword = "g", Url = "https://www.google.com/search?q=%s" };

        Assert.AreEqual(expected, entry.Matches(filter));
    }

    [TestMethod]
    public void MatchesKeywordOnly()
    {
        var entry = new WebSearchShortcutDataEntry { Name = "Stack Overflow", Keyword = "so", Url = "https://stackoverflow.com/search?q=%s" };

        Assert.IsTrue(entry.Matches("so"));
        Assert.IsFalse(new WebSearchShortcutDataEntry { Name = "Bing", Url = "https://www.bing.com/search?q=%s" }.Matches("so"));
    }
}
