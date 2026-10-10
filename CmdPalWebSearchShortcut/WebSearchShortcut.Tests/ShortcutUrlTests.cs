using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebSearchShortcut.Tests;

[TestClass]
public class ShortcutUrlTests
{
    private static WebSearchShortcutDataEntry Shortcut(string url, string? replaceWhitespace = null, string? homePage = null) =>
        new() { Name = "Test", Url = url, ReplaceWhitespace = replaceWhitespace, HomePage = homePage };

    [TestMethod]
    [DataRow(null, "a+b%26c")]
    [DataRow("", "a+b%26c")]
    [DataRow("+", "a+b%26c")]
    [DataRow("%20", "a%20b%26c")]
    [DataRow("-", "a-b%26c")]
    [DataRow("_", "a_b%26c")]
    public void GetSearchUrlEncodesQueryAndReplacesWhitespace(string? replaceWhitespace, string expectedQuery)
    {
        var url = WebSearchShortcutDataEntry.GetSearchUrl(Shortcut("https://example.com/search?q=%s", replaceWhitespace), "a b&c");

        Assert.AreEqual("https://example.com/search?q=" + expectedQuery, url);
    }

    [TestMethod]
    public void ReplaceWhitespaceDoesNotTouchLiteralPlusSigns()
    {
        var url = WebSearchShortcutDataEntry.GetSearchUrl(Shortcut("https://example.com/?q=%s", "%20"), "c++ guide");

        Assert.AreEqual("https://example.com/?q=c%2B%2B%20guide", url);
    }

    [TestMethod]
    public void GetSearchUrlWithoutPlaceholderReturnsUrlUnchanged()
    {
        Assert.AreEqual("https://example.com/", WebSearchShortcutDataEntry.GetSearchUrl(Shortcut("https://example.com/"), "ignored"));
    }

    [TestMethod]
    [DataRow("https://www.google.com/search?q=%s", "https://www.google.com")]
    [DataRow("https://en.wikipedia.org/w/index.php?fulltext=1&search=%s", "https://en.wikipedia.org")]
    [DataRow("example.com/search?q=%s", "https://example.com")]
    [DataRow("http://localhost:8080/find/%s", "http://localhost:8080")]
    public void DomainReturnsSchemeAndAuthority(string url, string expected)
    {
        Assert.AreEqual(expected, Shortcut(url).Domain);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("https://[invalid")]
    public void DomainOfInvalidUrlIsEmptyInsteadOfThrowing(string url)
    {
        Assert.AreEqual(string.Empty, Shortcut(url).Domain);
    }

    [TestMethod]
    public void DomainOfNullUrlIsEmpty()
    {
        var shortcut = new WebSearchShortcutDataEntry { Url = null! };

        Assert.AreEqual(string.Empty, shortcut.Domain);
    }

    [TestMethod]
    public void HomePageIsPreferredOverDomain()
    {
        Assert.AreEqual("https://home.example.com", WebSearchShortcutDataEntry.GetHomePageUrl(Shortcut("https://example.com/?q=%s", homePage: " https://home.example.com ")));
    }

    [TestMethod]
    public void HomePageFallsBackToDomain()
    {
        Assert.AreEqual("https://example.com", WebSearchShortcutDataEntry.GetHomePageUrl(Shortcut("https://example.com/?q=%s")));
    }

    [TestMethod]
    public void HomePageIsEmptyWhenNothingUsable()
    {
        Assert.AreEqual(string.Empty, WebSearchShortcutDataEntry.GetHomePageUrl(Shortcut("https://[invalid")));
    }
}
