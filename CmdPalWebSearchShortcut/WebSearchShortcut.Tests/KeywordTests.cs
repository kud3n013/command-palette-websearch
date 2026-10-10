using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebSearchShortcut.Tests;

[TestClass]
public class KeywordTests
{
    [TestMethod]
    [DataRow("g", "g cats", "cats")]
    [DataRow("g", "G cats", "cats")]
    [DataRow("g", "g   cats  dogs", "cats  dogs")]
    [DataRow("g", "  g cats", "cats")]
    [DataRow("g", "g", "")]
    [DataRow("g", "g ", "")]
    [DataRow(" yt ", "yt music", "music")]
    public void MatchingKeywordIsStripped(string keyword, string query, string expectedRemainder)
    {
        var shortcut = new WebSearchShortcutDataEntry { Keyword = keyword };

        Assert.IsTrue(WebSearchShortcutDataEntry.TryStripKeyword(shortcut, query, out var remainder));
        Assert.AreEqual(expectedRemainder, remainder);
    }

    [TestMethod]
    [DataRow("g", "go home")]
    [DataRow("g", "cats g")]
    [DataRow("g", "")]
    [DataRow("yt", "y")]
    [DataRow(null, "g cats")]
    [DataRow("", "g cats")]
    [DataRow("  ", "g cats")]
    public void NonMatchingQueryIsLeftAlone(string? keyword, string query)
    {
        var shortcut = new WebSearchShortcutDataEntry { Keyword = keyword };

        Assert.IsFalse(WebSearchShortcutDataEntry.TryStripKeyword(shortcut, query, out var remainder));
        Assert.AreEqual(query, remainder);
    }
}
