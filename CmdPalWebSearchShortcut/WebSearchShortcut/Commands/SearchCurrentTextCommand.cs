using System;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.Commands;

/// <summary>
/// Searches for whatever the page's search box holds at the moment of invocation, or opens the homepage when it's empty.
/// The host re-renders the list asynchronously, so pressing Enter right after typing can invoke an item built for older
/// text (e.g. the empty-state homepage item); reading the text at invoke time searches what the user actually typed.
/// </summary>
internal sealed partial class SearchCurrentTextCommand : InvokableCommand
{
    private readonly Func<string> _getSearchText;
    private readonly SearchWebCommand _searchWebCommand;
    private readonly OpenHomePageCommand _openHomePageCommand;

    public SearchCurrentTextCommand(WebSearchShortcutDataEntry shortcut, Func<string> getSearchText)
    {
        Name = $"[UNBOUND] {nameof(SearchCurrentTextCommand)}.{nameof(Name)} required - shortcut='{shortcut.Name}'";

        _getSearchText = getSearchText;
        _searchWebCommand = new SearchWebCommand(shortcut, string.Empty);
        _openHomePageCommand = new OpenHomePageCommand(shortcut);
    }

    public override CommandResult Invoke()
    {
        var searchText = _getSearchText();

        if (string.IsNullOrEmpty(searchText))
        {
            return _openHomePageCommand.Invoke();
        }

        _searchWebCommand.Query = searchText;

        return _searchWebCommand.Invoke();
    }
}
