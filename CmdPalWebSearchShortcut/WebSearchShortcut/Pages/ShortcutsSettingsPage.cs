using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Setting;

namespace WebSearchShortcut;

/// <summary>
/// The extension's settings page (Command Palette Settings → Extensions → WebSearchShortcut, or "Settings" in the
/// palette). Top to bottom: the "Add" and "Import and export" cards, the list heading with a search box, one expandable
/// card per shortcut (with an on/off switch), and the extension's own settings.
/// </summary>
/// <remarks>
/// Cards are kept between rebuilds so their state survives, and the page is only rebuilt when the set of visible cards
/// changes (a shortcut added or removed, a new search). Editing or switching a shortcut re-renders that card alone:
/// a rebuild re-creates every card, which collapses them and wipes whatever is typed in them.
/// </remarks>
internal sealed partial class ShortcutsSettingsPage : ContentPage
{
    private readonly ShortcutService _service;
    private readonly Lock _lock = new();

    private readonly AddShortcutCardForm _addCard;
    private readonly SearchCardForm _searchCard;
    private readonly ImportExportForm _importExportCard;
    private readonly LoadErrorForm _loadErrorCard;
    private readonly Dictionary<string, ShortcutCardForm> _shortcutCards = [];
    private string _filter = string.Empty;
    private string? _notice;

    public ShortcutsSettingsPage(ShortcutService service)
    {
        Id = "WebSearchShortcut.Settings";
        Name = Resources.SettingsItem_Title;
        Title = Resources.Settings_PageTitle;
        Icon = Icons.Logo;

        _service = service;
        _addCard = new AddShortcutCardForm(service);
        _searchCard = new SearchCardForm(SetFilter);
        _importExportCard = new ImportExportForm(service);
        _loadErrorCard = new LoadErrorForm(service, ShowNotice);

        _service.Changed += OnShortcutsChanged;
    }

    public override IContent[] GetContent()
    {
        var shortcuts = _service.GetShortcuts();
        List<IContent> content = [];

        lock (_lock)
        {
            if (_notice is not null)
                content.Add(new MarkdownContent(_notice));

            if (_service.LoadError is not null)
            {
                _loadErrorCard.Sync();
                content.Add(_loadErrorCard);
            }
            else
            {
                var visible = shortcuts.Where(s => s.Matches(_filter)).ToArray();

                content.Add(_addCard);
                content.Add(_importExportCard);
                _searchCard.Sync(shortcuts.Count, visible.Length, _filter);
                content.Add(_searchCard);
                content.AddRange(SyncShortcutCards(shortcuts, visible));
            }
        }

        content.AddRange(SettingsManager.Instance.Settings.ToContent());

        return [.. content];
    }

    private void OnShortcutsChanged(object? sender, ShortcutsChangedEventArgs e)
    {
        if (e.ListChanged)
        {
            RaiseItemsChanged();
            return;
        }

        // Values only: refresh the existing cards in place. Cards whose shortcut didn't change render the same JSON
        // and are left untouched.
        var shortcuts = _service.GetShortcuts();
        lock (_lock)
        {
            foreach (var shortcut in shortcuts)
            {
                if (_shortcutCards.TryGetValue(shortcut.Id, out var card))
                    card.Sync(shortcut);
            }
        }
    }

    // Must be called under _lock. Keeps a card for every shortcut (so hidden cards keep their state) and returns the
    // visible ones in list order.
    private List<ShortcutCardForm> SyncShortcutCards(IReadOnlyList<WebSearchShortcutDataEntry> shortcuts, IReadOnlyList<WebSearchShortcutDataEntry> visible)
    {
        var ids = shortcuts.Select(s => s.Id).ToHashSet();
        foreach (var staleId in _shortcutCards.Keys.Where(id => !ids.Contains(id)).ToArray())
            _shortcutCards.Remove(staleId);

        List<ShortcutCardForm> cards = [];
        foreach (var shortcut in shortcuts)
        {
            if (_shortcutCards.TryGetValue(shortcut.Id, out var card))
            {
                card.Sync(shortcut);
            }
            else
            {
                card = new ShortcutCardForm(shortcut, _service);
                _shortcutCards[shortcut.Id] = card;
            }

            if (visible.Contains(shortcut))
                cards.Add(card);
        }

        return cards;
    }

    private void SetFilter(string filter)
    {
        lock (_lock)
        {
            if (_filter == filter)
                return;

            _filter = filter;
        }

        RaiseItemsChanged();
    }

    private void ShowNotice(string notice)
    {
        lock (_lock)
        {
            _notice = notice;
        }

        RaiseItemsChanged();
    }
}
