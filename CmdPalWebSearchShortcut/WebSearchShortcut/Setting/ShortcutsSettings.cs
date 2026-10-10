using Microsoft.CommandPalette.Extensions;

namespace WebSearchShortcut.Setting;

/// <summary>
/// Gives Command Palette our own settings page (shortcut management) instead of the toolkit's
/// single-form settings page. The extension's regular settings are embedded in that page.
/// </summary>
internal sealed partial class ShortcutsSettings(IContentPage settingsPage) : ICommandSettings
{
    public IContentPage SettingsPage { get; } = settingsPage;
}
