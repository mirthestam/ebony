using Adw;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using Gdk;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Ebony.Features.Browser.Playlists;

[Subclass<NavigationPage>]
[Template<AssemblyResource>($"ui/{nameof(PlaylistsPage)}.ui")]
public partial class PlaylistsPage
{
    public enum PlaylistsPages
    {
        Playlists,
        Empty
    }
    
    private const string EmptyPageName = "empty-stack-page";
    private const string PlaylistsPageName = "playlists-stack-page";
    
    [Connect("playlists-column-view")] private ColumnView _columnView;
    [Connect("name-column")] ColumnViewColumn _nameColumn;
    [Connect("modified-column")] ColumnViewColumn _modifiedColumn;

    [Connect("playlists-stack")] private Stack _stack;
    
    [Connect("playlist-popover-menu")] private PopoverMenu _playlistPopoverMenu;
    
    // Raw Storage
    private ListStore _listModel;
    
    // Selection
    private SingleSelection _selection;
    
    public IPlaylistNameValidator NameValidator { get; set; }
    
    partial void Initialize()
    {
        InitializeColumnView();
        InitializeActions();
    }

    public void TogglePage(PlaylistsPages page)
    {
        var pageName = page switch
        {
            PlaylistsPages.Playlists => PlaylistsPageName,
            PlaylistsPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _stack.VisibleChildName = pageName;
    }

    public void ShowPlaylists(List<PlaylistModel> models)
    {
        Clear();
        foreach (var model in models)
        {
            _listModel.Append(model);
        }
    }    

    private PlaylistModel? GetSelectedPlaylist()
    {
        var selectedIndex = _selection.GetSelected();
        if (selectedIndex == Gtk.Constants.INVALID_LIST_POSITION) return null;
        var model =  _listModel.GetObject(selectedIndex) as PlaylistModel;
        return model;        
    }
    
    private void InitializeColumnView()
    {
        // Raw Data
        _listModel = ListStore.New(PlaylistModel.GetGType());
        
        // Sorting
        var nameSorter = CustomSorter.New<PlaylistModel>((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        var dateSorter = CustomSorter.New<PlaylistModel>((a, b) => a.LastModified.CompareTo(b.LastModified));
        _nameColumn.SetSorter(nameSorter);
        _modifiedColumn.SetSorter(dateSorter);
        var model = SortListModel.New(_listModel, _columnView.Sorter);
        
        // Selection
        _selection = SingleSelection.New(model);
        _selection.Autoselect = false;
        _columnView.SetModel(_selection);
        
        // Presentation
        var nameColumnFactory = (SignalListItemFactory) _nameColumn.Factory!;
        nameColumnFactory.OnSetup += NameColumnSetup;
        nameColumnFactory.OnTeardown += NameColumnOnTeardown;
        nameColumnFactory.OnBind += NameColumnBind;
        nameColumnFactory.OnUnbind += NameColumnUnbind;
        
        var modifiedColumnFactory =(SignalListItemFactory) _modifiedColumn.Factory!;
        modifiedColumnFactory.OnSetup += ModifiedColumnSetup;
        modifiedColumnFactory.OnTeardown += ModifiedColumnOnTeardown;
        modifiedColumnFactory.OnBind += ModifiedColumnBind;
        
        _columnView.OnActivate += ColumnViewOnOnActivate;
    }
    
    private void ColumnViewOnOnActivate(ColumnView sender, ColumnView.ActivateSignalArgs args)
    {
        ActivateSelection();
    }

    private void ActivateSelection()
    {
        if (_selection.SelectedItem is not PlaylistModel selectedModel) return;
        
        var argument = Variant.NewString(selectedModel.PlaylistId.ToString());
        var argumentArray = Variant.NewArray(VariantType.String, [argument]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", argumentArray);    
    }
    
    // Modified Column
    private static void ModifiedColumnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var label = Label.NewWithProperties([]);
        label.SetXalign(1);
        listItem.SetChild(label);
    }
    private void ModifiedColumnOnTeardown(SignalListItemFactory sender, SignalListItemFactory.TeardownSignalArgs args)
    {
    }
    private static void ModifiedColumnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var model = (PlaylistModel)listItem.GetItem()!;
        var label = (Label)listItem.GetChild()!;
        label.Label_ = model.LastModified.ToShortDateString();
    }
    
    // Name Column
    private void NameColumnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var cell = PlaylistNameCell.NewWithProperties([]);
        
        cell.GestureClick.OnReleased += NameColumnOnClickReleased;
        cell.GestureLongPress.OnPressed += NameColumnOnLongPressed;
        
        listItem.SetChild(cell);
    }
    private void NameColumnOnTeardown(SignalListItemFactory sender, SignalListItemFactory.TeardownSignalArgs args)
    {
        var item = (ListItem)args.Object;
        if (item.Child is not PlaylistNameCell child) return;

        // Gestures
        child.GestureClick.OnReleased -= NameColumnOnClickReleased;
        child.GestureLongPress.OnPressed -= NameColumnOnLongPressed;
        
        item.SetChild(null);        
    }
    private static void NameColumnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var model = (PlaylistModel)listItem.GetItem()!;
        var cell = (PlaylistNameCell)listItem.GetChild()!;
        cell.Bind(model);
    }    
    private void NameColumnUnbind(SignalListItemFactory sender, SignalListItemFactory.UnbindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var cell = (PlaylistNameCell)listItem.GetChild()!;
        cell.Unbind();
    }
    
    private void NameColumnOnLongPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        if (sender.Widget is not PlaylistNameCell cell) return;
        _listModel.Find(cell.Model!, out var position);
        
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);
        
        ShowPlaylistContextMenu(cell, args.X, args.Y);
    }
    private void NameColumnOnClickReleased(GestureClick sender, GestureClick.ReleasedSignalArgs args)
    {
        if (sender.Widget is not PlaylistNameCell cell) return;
        _listModel.Find(cell.Model!, out var position);
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);

        var button = sender.GetCurrentButton();
        switch (button)
        {
            case Gdk.Constants.BUTTON_PRIMARY:
                ActivateSelection();
                break;

            case Gdk.Constants.BUTTON_SECONDARY:
                ShowPlaylistContextMenu(cell, args.X, args.Y);
                break;
        }
    }

    private void ShowPlaylistContextMenu(PlaylistNameCell cell, double x, double y)
    {
        var selected = _selection.GetSelected();
        if (selected == Gtk.Constants.INVALID_LIST_POSITION) return;
    
        var pointInItem = new Graphene.Point { X = (float)x, Y = (float)y };
    
        if (!cell.ComputePoint(_columnView, pointInItem, out var pointInListView))
            return;
    
        var rect = new Rectangle();
        rect.X = (int)Math.Round(pointInListView.X);
        rect.Y = (int)Math.Round(pointInListView.Y);
        rect.Width = 1;
        rect.Height = 1;
    
        _playlistPopoverMenu.SetPointingTo(rect);
    
        if (!_playlistPopoverMenu.Visible)
            _playlistPopoverMenu.Popup();
    }
    
    private void Clear()
    {
        _listModel.RemoveAll();
    }
}