using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;

namespace WebSearchShortcut;

internal sealed partial class AddShortcutPage : ContentPage
{
    private readonly WebSearchShortcutDataEntry? _shortcut;
    private readonly ShortcutService _service;

    public AddShortcutPage(WebSearchShortcutDataEntry? shortcut, ShortcutService service)
    {
        bool isAdd = shortcut is null;

        Id = "WebSearchShortcut.AddShortcut";
        Title = isAdd ? Resources.AddShortcutPage_Title_Add : Resources.AddShortcutPage_Title_Edit;
        Name = $"[UNBOUND] {nameof(AddShortcutPage)}.{nameof(Name)} required - shortcut={(shortcut is null ? "null" : $"'{shortcut.Name}'")}";
        Icon = isAdd ? Icons.AddShortcut : Icons.EditShortcut;

        _shortcut = shortcut;
        _service = service;
    }

    // A fresh form each time the page opens, so a previous visit's half-filled values don't linger.
    public override IContent[] GetContent() => [new AddShortcutForm(_shortcut, _service)];
}
