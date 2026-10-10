# WebSearchShortcut for Command Palette

A [PowerToys Command Palette](https://learn.microsoft.com/windows/powertoys/command-palette/overview) extension for
searching any website straight from the palette: pick a search engine (or type its keyword), type your query, get
live suggestions, press Enter.

This is a fork of [Daydreamer-riri/CmdPal-WebSearchShortcut](https://github.com/Daydreamer-riri/CmdPal-WebSearchShortcut)
with shortcut management in Command Palette settings, batch import/export, keywords, and a number of fixes.

![Preview of the extension in action](./ScreenShots/preview-cmdpal.gif)

## Features

- **Any website**: add a search engine with a URL template such as `https://www.google.com/search?q=%s`.
- **Aliases**: give a shortcut an alias (e.g. `g`) and type `g cats` anywhere in the palette to search it.
- **Live suggestions** from Google, Bing, DuckDuckGo, YouTube, Wikipedia (in the shortcut's language), npm and Can I use.
- **Manage everything from Command Palette settings**: one searchable list where every search engine has an on/off
  switch and expands to its settings; add, edit and delete them, and **import or export** them in bulk as JSON
  (including configs from the old PowerToys Run plugin).
- **Global results per engine**: choose which engines are offered for anything you type, and which only answer to
  their alias.
- **Per-shortcut browser**: open a shortcut in a specific browser, with custom arguments (e.g. a private window).
- Ships with Google, Bing, YouTube, DuckDuckGo, Wikipedia and npm. Available in English, French and Chinese
  (Simplified and Traditional).

## Installation

> **Not published yet.** There is no Store or winget package for this fork yet; see [RELEASING.md](RELEASING.md)
> for the status. The `Riri.WebSearchShortcut` Store and winget packages are the original project, not this fork.

Until then, build it and register it as a development package:

1. Turn on **Developer Mode** (Settings → System → For developers).
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.10`).
3. Build and register:
   ```powershell
   git clone https://github.com/kud3n013/command-palette-websearch
   cd command-palette-websearch\CmdPalWebSearchShortcut
   dotnet build WebSearchShortcut\WebSearchShortcut.csproj -c Debug -p:Platform=x64
   Add-AppxPackage -Register WebSearchShortcut\bin\x64\Debug\AppxManifest.xml
   ```
   On Arm devices use `-p:Platform=ARM64` and `bin\ARM64\Debug\AppxManifest.xml`. Keep `-c Debug`: otherwise the
   project's publish profile makes a plain build use Release, leaving `bin\x64\Debug` stale or missing.
4. Open Command Palette and run **Reload** ("Reload Command Palette extensions").

To remove it: `Get-AppxPackage kud3n013.WebSearchShortcut | Remove-AppxPackage`.

With Visual Studio you can instead open `CmdPalWebSearchShortcut/WebSearchShortcut.sln`, pick the **x64** platform and
the **WebSearchShortcut (Package)** profile, and use **Build → Deploy**.

## Usage

- Type a shortcut's name (e.g. `Google`) and press Enter to open its search page, then type your query. Suggestions
  appear as you type; press Enter on the query or any suggestion to search.
- Or type `<alias> <query>` (e.g. `g cats`) from the palette's home page.
- Any query also offers each shortcut that has **Include in global results** on as a fallback ("Search Google for …").
- The context menu of a shortcut (Ctrl+K by default) has **Open homepage**, **Edit**, **Delete** and **Settings**.

## Managing search shortcuts

### In Command Palette settings

Open **Command Palette Settings → Extensions → WebSearchShortcut**, or pick **Settings** from a shortcut's context
menu. From top to bottom:

- **Add Search Shortcut**: click it to open the form, fill it in and press **Add**.
- **Import and export**: collapsed until clicked; see [Batch import](#batch-import).
- **Your search shortcuts**: type in the search box and press **Search** to filter the list by name, alias or URL;
  **Show all** clears the filter.
- **One card per search shortcut**, showing its icon, name, alias and URL:
  - the **switch** turns it on or off. A shortcut that is off stays in the list but disappears from Command Palette
    (its page, its alias and its fallback);
  - clicking the card expands it to every field (name, URL, alias, suggestion provider, **Include in global results**, homepage,
    whitespace replacement, browser and browser arguments) with **Save** and **Delete** (deleting asks for
    confirmation inside the card).
- The extension's log level.

### From the palette

Choose **Add Search Shortcut** in the palette, fill in the form and save:

![Add a search shortcut](./ScreenShots/cmdpal-add.png)

### Batch import

In the **Import and export** card of the settings page, paste JSON or the full path to a `.json` file and press
**Import**. Accepted formats:

| Format | Example |
|--------|---------|
| An export from the settings page, or a `WebSearchShortcut.json` file | `{ "Data": [ { "Name": "Google", "Url": "https://www.google.com/search?q=%s" } ] }` |
| A list of shortcuts | `[ { "Name": "Google", "Url": "..." }, { "Name": "Bing", "Url": "..." } ]` |
| A single shortcut | `{ "Name": "Google", "Url": "..." }` |
| A PowerToys Run WebSearchShortcut config | `{ "Google": { "Url": "...", "Keyword": "g" } }` |

Field names are case-insensitive and are the [configuration fields](#configuration-fields) below. Every shortcut
needs a `Name` and a `Url`, and a `Keyword` (alias) without spaces; the result message tells you how many were skipped as
invalid. Unknown `SuggestionProvider` values are dropped. JSON comments and trailing commas are allowed.

When a shortcut with the same name already exists it is skipped, unless **Replace shortcuts that have the same name**
is on (the existing shortcut keeps its identity, so Command Palette aliases and hotkeys assigned to it keep working).

**Export** copies all shortcuts to the clipboard in the first format, which can be imported again.

## Configuration fields

Shortcuts are stored in `WebSearchShortcut.json` in the extension's package data folder
(`%LOCALAPPDATA%\Packages\kud3n013.WebSearchShortcut_5ncnfbw6m6sya\LocalState\`). You normally edit them in the settings
page, but the file can also be edited by hand. If it ever becomes invalid, the extension shows an error item that
can open the file, reload it, or reset it to the defaults (the broken file is kept as a `.bak` copy).

```json
{
  "Data": [
    {
      "Name": "Google",
      "Url": "https://www.google.com/search?q=%s",
      "Keyword": "g",
      "SuggestionProvider": "Google"
    },
    {
      "Name": "Wikipedia (FR)",
      "Url": "https://fr.wikipedia.org/w/index.php?fulltext=1&search=%s",
      "SuggestionProvider": "Wikipedia",
      "ReplaceWhitespace": "%20"
    },
    {
      "Name": "GitHub (private window)",
      "Url": "https://github.com/search?q=%s",
      "BrowserPath": "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
      "BrowserArgs": "--inprivate %1"
    }
  ]
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `Name` | yes | Name shown in the palette. |
| `Url` | yes | [URL template](#url) with `%s` for the query. |
| `Keyword` | no | The alias: a [short prefix](#keyword) for searching from the palette's home page. |
| `SuggestionProvider` | no | [Suggestion source](#suggestionprovider). |
| `ReplaceWhitespace` | no | [Separator](#replacewhitespace) used instead of `+` between words. |
| `HomePage` | no | Page opened by **Open homepage** (and when the query is empty). Defaults to the scheme and host of `Url`. |
| `BrowserPath` | no | Browser executable to use instead of the default browser. The settings form lists installed browsers. |
| `BrowserArgs` | no | Arguments for that browser; `%1` is replaced by the URL (appended if missing), e.g. `--inprivate %1`. |
| `Enabled` | no | `false` hides the shortcut from Command Palette without deleting it (the switch on the settings page). Default `true`. |
| `UseAsFallback` | no | **Include in global results**: offer this shortcut for anything typed in Command Palette. With `false` it is only searched through its own page or its alias. Default `true`. |
| `IconUrl` | no | Icon to show. Filled in automatically from the site's favicon. |
| `Id` | — | Generated automatically. Command Palette attaches aliases, hotkeys and fallback settings to it. |

### `Url`

The URL template for performing the search. Use `%s` as a placeholder for the search query. If the URL does not
contain `%s`, the URL is opened directly when you press Enter.

### `Keyword`

Optional short prefix (no spaces), labeled **Alias** in the forms. Typing `<alias> <query>` in Command Palette, e.g.
`g cats`, offers to search that shortcut for just `cats`, whether or not it is included in global results. The alias is shown
next to the shortcut in the palette, e.g. "Search Google (g …)".

This is separate from Command Palette's own aliases (set per command in **Command Palette Settings → Extensions**),
which open the shortcut's page instead of searching directly. Command Palette's own per-fallback switches also apply
on top of **Include in global results**: a shortcut is offered for any query only when both are on.

### `ReplaceWhitespace`

With `ReplaceWhitespace`, you can specify which character(s) to replace a space with when performing a search. This
is useful for some websites, such as Wikipedia, which don't use plus signs ("+") to separate words in the URL.

| Value          | Result             |
|----------------|--------------------|
| `""` (Default) | `Example+search`   |
| `"+"`          | `Example+search`   |
| `"%20"`        | `Example%20search` |
| `"-"`          | `Example-search`   |
| `"_"`          | `Example_search`   |

> **Note**: The character defined in `ReplaceWhitespace` will be used **literally** as the separator without
> further encoding. While some modern browsers may automatically encode invalid characters, users are responsible
> for ensuring their chosen character is valid and compatible with the target URL template. For more information on
> valid URL characters, please refer to [percent-encoding](https://en.wikipedia.org/wiki/Percent-encoding).

### `SuggestionProvider`

Where search suggestions come from while you type. Supported providers:

- `Google`
- `Bing`
- `DuckDuckGo`
- `YouTube`
- `Wikipedia` (uses the language of the shortcut's URL, e.g. `fr.wikipedia.org`; English otherwise)
- `Npm`
- `CanIUse`

Suggestions are requested once you pause typing, and time out after 5 seconds.

A provider doesn't have to match the site: you can, for example, use Google suggestions for any search engine.

![Choosing a suggestion provider](./ScreenShots/cmdpal-add-suggestions.png)

Planned: `Yandex`, `Baidu`. PRs welcome!

## Development

Requirements: Windows 10 2004 (build 19041) or later, the .NET 10 SDK, and Developer Mode for registering the dev
package. The extension targets .NET 9 and the Command Palette extensions SDK.

```powershell
cd CmdPalWebSearchShortcut
dotnet build WebSearchShortcut.sln -p:Platform=x64
dotnet test WebSearchShortcut.Tests/WebSearchShortcut.Tests.csproj -p:Platform=x64
```

After changing code, stop the running extension (Command Palette keeps it loaded, which locks the files in
`bin\x64\Debug` and makes the build fail with "file is being used by another process"), rebuild, re-register, and
**Reload** Command Palette:

```powershell
Stop-Process -Name WebSearchShortcut -ErrorAction SilentlyContinue
dotnet build WebSearchShortcut\WebSearchShortcut.csproj -c Debug -p:Platform=x64
Add-AppxPackage -Register WebSearchShortcut\bin\x64\Debug\AppxManifest.xml
```

Project layout (`CmdPalWebSearchShortcut/WebSearchShortcut/`):

| Path | What it is |
|------|------------|
| `Program.cs`, `WebSearchShortcut.cs` | COM server entry point and the `IExtension` Command Palette activates. |
| `WebSearchShortcutCommandsProvider.cs` | Top-level commands, fallback items and the settings page. |
| `Services/ShortcutService.cs` | Owns the shortcuts file: every add/edit/delete/import goes through it. |
| `Services/ShortcutImporter.cs` | Parses and merges imported JSON. |
| `Storage.cs`, `WebSearchShortcutDataEntry.cs` | The shortcuts file and its entries. |
| `Pages/` | The search page, fallback items, add/edit page and the settings page. |
| `Forms/` | Adaptive Card forms; `Forms/Settings/` holds the settings page cards. |
| `SuggestionsProviders/` | One class per suggestion source (see `HttpSuggestionsProvider`). |
| `Browser/` | Default and installed browser detection. |

The tests (`WebSearchShortcut.Tests`) compile the logic files directly instead of referencing the app, so they run
without installing anything. Release packaging is described in [RELEASING.md](RELEASING.md).

## Credits

- Forked from [Daydreamer-riri/CmdPal-WebSearchShortcut](https://github.com/Daydreamer-riri/CmdPal-WebSearchShortcut)
  by [@Daydreamer-riri](https://github.com/Daydreamer-riri), which grew out of the PowerToys Run WebSearchShortcut plugin.
- The original project was built with the guidance of [this article](https://conductofcode.io/post/creating-custom-powertoys-run-plugins/).
  Thanks to @hlaueriksson for his great work.
- The search suggestion feature is based on the implementation in
  [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher?tab=readme-ov-file#web-searches--urls). Thanks @Flow-Launcher!
- Thanks to [@thatgaypigeon](https://github.com/thatgaypigeon) for the original documentation and
  [@Tortoise](https://github.com/Testudinidae) for many refactorings and improvements to the original project.

## License

[MIT](./LICENSE). © 2024 [Riri](https://github.com/Daydreamer-riri); fork changes © 2026 [kud3n013](https://github.com/kud3n013).
