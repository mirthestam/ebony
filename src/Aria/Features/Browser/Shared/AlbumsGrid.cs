using Aria.Infrastructure;
using Gdk;
using Gio;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Shared;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(AlbumsGrid)}.ui")]
public partial class AlbumsGrid
{
    // TODO: Context Menu
    // TODO: Activation
    
    public enum AlbumSorting
    {
        Title,
        TitleDescending,
        ReleaseDate,
        ReleaseDateDescending
    }
    
    [Connect("albums-grid-view")] private GridView _gridView;
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;
    
    // Raw storage
    private ListStore _listModel;    
    
    // Sorting
    private CustomSorter _sorter;
    private SortListModel _sortedListModel;
    private AlbumSorting _sorting = AlbumSorting.Title;
    
    // Selection
    private SingleSelection _selection;
    public event EventHandler<AlbumActivatedEventArgs> AlbumActivated;
    
    // Presentation
    private SignalListItemFactory _itemFactory;

    public AlbumModel? GetSelected()
    {
        var selected = _selection.GetSelected();
        if (selected == Gtk.Constants.INVALID_LIST_POSITION) return null;
        var item =  _listModel.GetObject(selected) as AlbumModel;
        return item;
    }

    public void RemoveAll()
    {
        _listModel.RemoveAll();
    }

    public void SetAlbumMenu(MenuModel? model)
    {
         _albumPopoverMenu.SetMenuModel(model);
    }
    
    public void Append(IReadOnlyList<AlbumModel> albumModels)
    {
        foreach (var album in albumModels) _listModel.Append(album);        
    }
    
    public void SortBy(AlbumSorting filter)
    {
        _sorting = filter;

        //_sorterAction.SetState(Variant.NewString(filter.ToString()));

        // Tell the actual sorter our preference has changed
        _sorter.Changed(SorterChange.Different);
    }    
    
    partial void Initialize()
    {
        // Raw Data
        _listModel = ListStore.New(AlbumModel.GetGType());

        // Sorting
        CompareDataFuncT<AlbumModel> sortAlbum = SortAlbum;
        _sorter = CustomSorter.New(sortAlbum);
        _sortedListModel = SortListModel.New(_listModel, _sorter);
        
        // Selection
        _selection = SingleSelection.New(_sortedListModel);   
        
        // Presentation
        _itemFactory = SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnTeardown += ItemFactoryOnOnTeardown;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;
        _itemFactory.OnUnbind += ItemFactoryOnOnUnbind;

        _gridView.SetFactory(_itemFactory);
        _gridView.SetModel(_selection);
        
        _gridView.OnActivate += GridViewOnOnActivate;
    }
    
    private void GridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_selection.SelectedItem is not AlbumModel selectedModel) return;
        OnAlbumActivated(selectedModel);
    }

    private int SortAlbum(AlbumModel a, AlbumModel b)
    {
        switch (_sorting)
        {
            default:
            case AlbumSorting.Title:
                return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase) switch
                {
                    < 0 => -1,
                    > 0 => 1,
                    _ => 0
                };

            case AlbumSorting.TitleDescending:
                return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase) switch
                {
                    < 0 => 1,
                    > 0 => -1,
                    _ => 0
                };
            case AlbumSorting.ReleaseDate:
                return a.ReleaseDate?.CompareTo(b.ReleaseDate) ?? 0;

            case AlbumSorting.ReleaseDateDescending:
                return b.ReleaseDate?.CompareTo(a.ReleaseDate) ?? 0;
        }
    }
    
    private void ShowAlbumContextMenu(AlbumListItem listItem, double x, double y)
    {
        var selected = _selection.GetSelected();
        if (selected == Gtk.Constants.INVALID_LIST_POSITION) return;
    
        var pointInItem = new Graphene.Point { X = (float)x, Y = (float)y };
    
        if (!listItem.ComputePoint(_gridView, pointInItem, out var pointInListView))
            return;
    
        var rect = new Rectangle();
        rect.X = (int)Math.Round(pointInListView.X);
        rect.Y = (int)Math.Round(pointInListView.Y);
        rect.Width = 1;
        rect.Height = 1;
    
        _albumPopoverMenu.SetPointingTo(rect);
    
        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();
    }    
    
    private void OnItemFactoryOnOnSetup(SignalListItemFactory factory, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = AlbumListItem.NewWithProperties([]);

        // Gestures
        child.GestureClick.OnReleased += GestureClickOnOnReleased;
        child.GestureLongPress.OnPressed += GestureLongPressOnOnPressed;

        item.SetChild(child);
    }
    
    private void GestureClickOnOnReleased(GestureClick sender, GestureClick.ReleasedSignalArgs args)
    {
        if (sender.Widget is not AlbumListItem listItem) return;
        _listModel.Find(listItem.Model!, out var position);
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);

        var button = sender.GetCurrentButton();
        switch (button)
        {
            case Gdk.Constants.BUTTON_PRIMARY:
                OnAlbumActivated(listItem.Model!);
                break;

            case Gdk.Constants.BUTTON_SECONDARY:
                ShowAlbumContextMenu(listItem, args.X, args.Y);
                break;
        }
    }

    private void OnAlbumActivated(AlbumModel album)
    {
        AlbumActivated?.Invoke(this, new AlbumActivatedEventArgs(album));
    }

    private void GestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        if (sender.Widget is not AlbumListItem listItem) return;
        _listModel.Find(listItem.Model!, out var position);
        
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);
        
        ShowAlbumContextMenu(listItem, args.X, args.Y);
    }
    
    private void ItemFactoryOnOnTeardown(SignalListItemFactory sender, SignalListItemFactory.TeardownSignalArgs args)
    {
        var item = (ListItem)args.Object;
        if (item.Child is not AlbumListItem child) return;

        // Gestures
        child.GestureClick.OnReleased -= GestureClickOnOnReleased;
        child.GestureLongPress.OnPressed -= GestureLongPressOnOnPressed;
        
        item.SetChild(null);        
    }
    
    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        if (listItem.GetItem() is not AlbumModel model)
        {
            return;
        }

        if (listItem.GetChild() is not AlbumListItem albumListItem)
        {
            return;
        }

        albumListItem.Bind(model);
    }
    
    private static void ItemFactoryOnOnUnbind(SignalListItemFactory sender, SignalListItemFactory.UnbindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        if (listItem.GetChild() is not AlbumListItem albumListItem)
        {
            return;
        }

        albumListItem.Unbind();
    }    
}

public class AlbumActivatedEventArgs(AlbumModel album) : EventArgs
{
    public AlbumModel Album { get; set; } = album;
}