using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.SuggestionsProviders;

namespace WebSearchShortcut.Tests;

/// <summary>
/// Parses canned responses shaped like each endpoint's real output; no network access.
/// </summary>
[TestClass]
public class SuggestionParsingTests
{
    private static string Titles(Suggestion[] suggestions) => string.Join("|", suggestions.Select(s => s.Title));

    [TestMethod]
    public void Google()
    {
        var result = SuggestionsProviders.Google.Parse("""["cats",["cats","cats movie"],[],[],{}]""");

        Assert.AreEqual("cats|cats movie", Titles(result));
    }

    [TestMethod]
    public void Bing()
    {
        var result = SuggestionsProviders.Bing.Parse("""
            {"AS":{"Query":"cats","FullResults":1,"Results":[
              {"Type":"AS","Suggests":[{"Txt":"cats","Type":"AS"},{"Txt":"cats movie","Type":"AS"}]}
            ]}}
            """);

        Assert.AreEqual("cats|cats movie", Titles(result));
    }

    [TestMethod]
    public void BingWithNoResults()
    {
        Assert.IsEmpty(SuggestionsProviders.Bing.Parse("""{"AS":{"Query":"zzzz","FullResults":0}}"""));
    }

    [TestMethod]
    public void DuckDuckGoSkipsTheQueryItself()
    {
        var result = DuckDuckGo.Parse("""[{"phrase":"cats"},{"phrase":"cats movie"},{"phrase":""}]""", "Cats");

        Assert.AreEqual("cats movie", Titles(result));
    }

    [TestMethod]
    public void YouTubeJsonp()
    {
        var result = YouTube.Parse("""window.google.ac.h(["cats",[["cats",0,[512]],["cats song",0,[512]]],{"k":1,"q":"x"}])""");

        Assert.AreEqual("cats|cats song", Titles(result));
    }

    [TestMethod]
    public void YouTubeUnexpectedBodyYieldsNothing()
    {
        Assert.IsEmpty(YouTube.Parse("<html>blocked</html>"));
    }

    [TestMethod]
    public void NpmIncludesDescriptionAndVersionTag()
    {
        var result = Npm.Parse("""
            {"objects":[
              {"package":{"name":"react","description":"UI library","version":"19.0.0"}},
              {"package":{"name":"left-pad"}}
            ]}
            """);

        Assert.HasCount(2, result);
        Assert.AreEqual("UI library", result[0].Description);
        Assert.AreEqual("19.0.0", result[0].Tags?.Single());
        Assert.AreEqual("", result[1].Description);
        Assert.IsNull(result[1].Tags);
    }

    [TestMethod]
    public void CanIUseIsCappedAtTen()
    {
        var ids = string.Join(",", Enumerable.Range(1, 12).Select(i => $"\"feature-{i}\""));

        Assert.HasCount(10, CanIUse.Parse($$"""{"featureIds":[{{ids}}]}"""));
    }

    [TestMethod]
    public void WikipediaThumbnailsAndDescriptions()
    {
        var result = Wikipedia.Parse("""
            {"pages":[
              {"title":"Cat","description":"Small domesticated carnivorous mammal","thumbnail":{"url":"//upload.wikimedia.org/cat.jpg"}},
              {"title":"Cats (musical)","description":null,"thumbnail":null},
              {"title":""}
            ]}
            """);

        Assert.HasCount(2, result);
        Assert.AreEqual("https://upload.wikimedia.org/cat.jpg", result[0].Image);
        Assert.AreEqual("Small domesticated carnivorous mammal", result[0].Description);
        Assert.AreEqual("", result[1].Description);
        Assert.IsNull(result[1].Image);
    }

    [TestMethod]
    [DataRow("https://en.wikipedia.org/w/index.php?fulltext=1&search=%s", "en")]
    [DataRow("https://fr.wikipedia.org/w/index.php?search=%s", "fr")]
    [DataRow("https://FR.Wikipedia.org/wiki/%s", "fr")]
    [DataRow("https://de.m.wikipedia.org/w/index.php?search=%s", "de")]
    [DataRow("https://zh-yue.wikipedia.org/w/index.php?search=%s", "zh-yue")]
    [DataRow("https://www.wikipedia.org/search-redirect.php?search=%s", "en")]
    [DataRow("https://fr.wikipedia.org.evil.example/?q=%s", "en")]
    [DataRow("https://evil.example/fr.wikipedia.org?q=%s", "en")]
    [DataRow("https://www.google.com/search?q=%s", "en")]
    [DataRow("", "en")]
    [DataRow(null, "en")]
    public void WikipediaLanguageComesFromShortcutUrl(string? url, string expected)
    {
        Assert.AreEqual(expected, Wikipedia.GetLanguage(url));
    }
}
