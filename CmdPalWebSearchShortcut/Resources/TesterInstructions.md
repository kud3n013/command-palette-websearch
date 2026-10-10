## Prerequisites

### System Requirements
- Windows 11, or Windows 10 version 2004 (build 19041) or newer
- x64 or ARM64 processor

### Required Software
- **PowerToys Command Palette**: install PowerToys from the Microsoft Store
  (https://apps.microsoft.com/detail/xp89dcgq3k6vld) or with `winget install Microsoft.PowerToys`, then enable
  Command Palette and set a hotkey.

## Installation

Install the extension package for your architecture (`WebSearchShortcut-x.y.z-x64.msix` or `-arm64.msix`) by
double-clicking it, or with `Add-AppxPackage <file>`. Then open Command Palette, type **Reload** and run
"Reload Command Palette extensions".

## Testing the Extension

### Searching
1. Open Command Palette, type `google` (or `bing`, `youtube`, `wikipedia`, `npm`...) and press Enter.
2. Type a query. Suggestions appear as you type.
3. Press Enter on the query or a suggestion. The browser opens the search results.

### Aliases
1. In Command Palette Settings → Extensions → WebSearchShortcut, click the **Google** card to expand it, set its
   alias to `g` and press **Save**.
2. From the palette's home page type `g cats`. The Google entry offers to search for "cats".

### Managing shortcuts (Command Palette Settings → Extensions → WebSearchShortcut)
1. **Add**: click **Add Search Shortcut** at the top, enter Name `Test` and Url `https://example.com/?q=%s`, press
   **Add**. It appears in the list and in the palette.
2. **Search**: type `test` in the search box and press **Search**. Only the Test card is listed; **Show all** brings
   the others back.
3. **Switch**: turn Test off with the switch on its card. It disappears from the palette; turn it back on.
4. **Edit**: click the Test card, change the Url and press **Save**. Uncheck **Include in global results** and save: typing
   any text in the palette no longer offers Test, but its alias still does.
5. **Delete**: in the expanded card press **Delete**, then confirm.
6. **Import**: click **Import and export**, paste
   `[{"Name":"StackOverflow","Url":"https://stackoverflow.com/search?q=%s","Keyword":"so"}]` and press Import.
   A result message reports 1 added.
7. **Export**: press "Copy all as JSON" and paste the clipboard somewhere to check it.

### Expected Behavior
- The default shortcuts (Google, Bing, YouTube, DuckDuckGo, Wikipedia, npm) appear in Command Palette.
- Pressing Enter on a query opens the default browser (or the browser chosen for that shortcut) with the search.
- Changes made in the settings page appear in the palette immediately, and vice versa.

## Troubleshooting

### Extension Not Appearing
- Run "Reload Command Palette extensions", or restart PowerToys.
- Check the package is installed: Settings > Apps > Installed apps, or `Get-AppxPackage *WebSearchShortcut*`.

## Reporting Issues

Please report problems at https://github.com/kud3n013/command-palette-websearch/issues with:
- Windows version and build number
- PowerToys version
- Exact error messages
- Steps to reproduce the issue
- Screenshots if applicable
