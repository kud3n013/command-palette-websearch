using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.Helpers;

namespace WebSearchShortcut.Tests;

[TestClass]
public class ShortcutEntryTests
{
    [TestMethod]
    public void CopyValuesFromKeepsId()
    {
        var target = new WebSearchShortcutDataEntry { Id = "keep-me", Name = "Old", Url = "https://old/%s", Keyword = "o" };
        var source = new WebSearchShortcutDataEntry
        {
            Id = "ignored",
            Name = "New",
            Url = "https://new/%s",
            Keyword = null,
            SuggestionProvider = "Google",
            HomePage = "https://new",
            IconUrl = "https://new/icon.png",
            ReplaceWhitespace = "-",
            BrowserPath = @"C:\b.exe",
            BrowserArgs = "%1",
        };

        target.CopyValuesFrom(source);

        Assert.AreEqual("keep-me", target.Id);
        Assert.AreEqual("New", target.Name);
        Assert.AreEqual("https://new/%s", target.Url);
        Assert.IsNull(target.Keyword);
        Assert.AreEqual("Google", target.SuggestionProvider);
        Assert.AreEqual("https://new", target.HomePage);
        Assert.AreEqual("https://new/icon.png", target.IconUrl);
        Assert.AreEqual("-", target.ReplaceWhitespace);
        Assert.AreEqual(@"C:\b.exe", target.BrowserPath);
        Assert.AreEqual("%1", target.BrowserArgs);
    }

    [TestMethod]
    [DataRow(null, true)]
    [DataRow("", true)]
    [DataRow("g", true)]
    [DataRow("c++", true)]
    [DataRow("two words", false)]
    [DataRow(" g", false)]
    [DataRow("tab\tbed", false)]
    public void KeywordValidation(string? keyword, bool valid)
    {
        Assert.AreEqual(valid, WebSearchShortcutDataEntry.IsValidKeyword(keyword));
    }

    [TestMethod]
    [DataRow("{}", "{\"action\":\"save\"}", "save")]
    [DataRow("{\"name\":\"x\",\"action\":\"edit\"}", "", "edit")]
    [DataRow("{\"action\":\"inputs\"}", "{\"action\":\"data\"}", "data")]
    [DataRow("{}", "{}", null)]
    [DataRow("not json", "also not json", null)]
    [DataRow(null, null, null)]
    public void SubmitActionIsReadFromDataThenInputs(string? inputs, string? data, string? expected)
    {
        Assert.AreEqual(expected, AdaptiveCardHelpers.GetSubmitAction(inputs, data));
    }
}
