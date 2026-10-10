using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Browser;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.Commands;

internal sealed partial class OpenHomePageCommand : InvokableCommand
{
    private readonly WebSearchShortcutDataEntry _shortcut;
    private readonly BrowserExecutionInfo _browserInfo;

    public OpenHomePageCommand(WebSearchShortcutDataEntry shortcut)
    {
        Name = $"[UNBOUND] {nameof(OpenHomePageCommand)}.{nameof(Name)} required - shortcut='{shortcut.Name}'";

        _shortcut = shortcut;
        _browserInfo = new BrowserExecutionInfo(shortcut);
    }

    public override CommandResult Invoke()
    {
        var url = WebSearchShortcutDataEntry.GetHomePageUrl(_shortcut);

        if (string.IsNullOrEmpty(url))
        {
            return CommandResult.ShowToast(StringFormatter.Format(Resources.NoHomepage_ToastTemplate, new() { ["shortcut"] = _shortcut.Name }));
        }

        if (!ShellHelpers.OpenCommandInShell(_browserInfo.Path, _browserInfo.ArgumentsPattern, url))
        {
            return CommandResult.ShowToast(StringFormatter.Format(Resources.OpenUrlFailed_ToastTemplate, new() { ["url"] = url }));
        }

        return CommandResult.Dismiss();
    }
}
