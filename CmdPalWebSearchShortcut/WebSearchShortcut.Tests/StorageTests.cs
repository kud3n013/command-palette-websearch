using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebSearchShortcut.Tests;

[TestClass]
public class StorageTests
{
    private string _directory = null!;
    private string _path = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "WebSearchShortcutTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "WebSearchShortcut.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void MissingFileIsCreatedWithDefaults()
    {
        var storage = Storage.ReadFromFile(_path);

        Assert.IsTrue(File.Exists(_path));
        Assert.HasCount(6, storage.Data);
        Assert.IsTrue(storage.Data.TrueForAll(entry => !string.IsNullOrWhiteSpace(entry.Id)));
    }

    [TestMethod]
    public void EmptyFileLoadsAsEmptyStorage()
    {
        File.WriteAllText(_path, "");

        Assert.IsEmpty(Storage.ReadFromFile(_path).Data);
    }

    [TestMethod]
    public void ShortcutsRoundTrip()
    {
        var original = new Storage();
        original.Data.Add(new WebSearchShortcutDataEntry
        {
            Name = "Google",
            Url = "https://www.google.com/search?q=%s",
            Keyword = "g",
            SuggestionProvider = "Google",
            ReplaceWhitespace = "%20",
            HomePage = "https://www.google.com",
            BrowserPath = @"C:\browser.exe",
            BrowserArgs = "--incognito %1",
        });

        Storage.WriteToFile(_path, original);
        var loaded = Storage.ReadFromFile(_path).Data[0];

        Assert.AreEqual(original.Data[0].Id, loaded.Id);
        Assert.AreEqual("Google", loaded.Name);
        Assert.AreEqual("https://www.google.com/search?q=%s", loaded.Url);
        Assert.AreEqual("g", loaded.Keyword);
        Assert.AreEqual("Google", loaded.SuggestionProvider);
        Assert.AreEqual("%20", loaded.ReplaceWhitespace);
        Assert.AreEqual("https://www.google.com", loaded.HomePage);
        Assert.AreEqual(@"C:\browser.exe", loaded.BrowserPath);
        Assert.AreEqual("--incognito %1", loaded.BrowserArgs);
    }

    [TestMethod]
    public void CorruptFileThrowsAndIsNotOverwritten()
    {
        const string corrupt = """{ "Data": [ { "Name": "Mine", """;
        File.WriteAllText(_path, corrupt);

        var ex = Assert.ThrowsExactly<StorageLoadException>(() => Storage.ReadFromFile(_path));

        Assert.AreEqual(_path, ex.FilePath);
        Assert.AreEqual(corrupt, File.ReadAllText(_path));
    }

    [TestMethod]
    public void NullEntriesAndFieldsAreRepaired()
    {
        File.WriteAllText(_path, """{ "Data": [ null, { "Name": null, "Url": null } ] }""");

        var storage = Storage.ReadFromFile(_path);

        Assert.HasCount(1, storage.Data);
        Assert.AreEqual(string.Empty, storage.Data[0].Name);
        Assert.AreEqual(string.Empty, storage.Data[0].Url);
        Assert.AreEqual(string.Empty, storage.Data[0].Domain);
    }

    [TestMethod]
    public void ResetBacksUpCurrentFileAndRestoresDefaults()
    {
        const string corrupt = "not json";
        File.WriteAllText(_path, corrupt);

        var backupPath = Storage.ResetToDefaults(_path);

        Assert.IsNotNull(backupPath);
        Assert.AreEqual(corrupt, File.ReadAllText(backupPath));
        Assert.HasCount(6, Storage.ReadFromFile(_path).Data);
    }

    [TestMethod]
    public void ResetWithoutExistingFileHasNoBackup()
    {
        Assert.IsNull(Storage.ResetToDefaults(_path));
        Assert.IsTrue(File.Exists(_path));
    }
}
