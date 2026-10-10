using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Browser;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.Commands;

internal sealed partial class SearchWebCommand : InvokableCommand
{
    public new string Id
    {
        get => base.Id;
        set => base.Id = value;
    }

    public string Query { get; internal set; } = string.Empty;

    private readonly WebSearchShortcutDataEntry _shortcut;
    private readonly BrowserExecutionInfo _browserInfo;
    // private readonly SettingsManager _settingsManager;

    public SearchWebCommand(WebSearchShortcutDataEntry shortcut, string query)
    {
        Name = $"[UNBOUND] {nameof(SearchWebCommand)}.{nameof(Name)} required - shortcut='{shortcut.Name}', query='{query}'";

        Query = query;

        _shortcut = shortcut;
        _browserInfo = new BrowserExecutionInfo(shortcut);
        // _settingsManager = settingsManager;
    }

    public override CommandResult Invoke()
    {
        var url = WebSearchShortcutDataEntry.GetSearchUrl(_shortcut, Query);

        if (!ShellHelpers.OpenCommandInShell(_browserInfo.Path, _browserInfo.ArgumentsPattern, url))
        {
            return CommandResult.ShowToast(StringFormatter.Format(Resources.OpenUrlFailed_ToastTemplate, new() { ["url"] = url }));
        }

        // if (_settingsManager.ShowHistory != Resources.history_none)
        // {
        //   _settingsManager.SaveHistory(new HistoryItem(Arguments, DateTime.Now));
        // }

        return CommandResult.Dismiss();
    }
}
