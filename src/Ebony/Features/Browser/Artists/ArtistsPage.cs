using Adw;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using Gio;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;
using Object = GObject.Object;

namespace Ebony.Features.Browser.Artists;

[Subclass<NavigationPage>]
[Template<AssemblyResource>($"ui/{nameof(ArtistsPage)}.ui")]
public partial class ArtistsPage
{
    [Connect("artists-list-view")] private ListView _artistsListView;
    [Connect("artists-menu-button")] private MenuButton _artistsMenuButton;
    [Connect("navigation-menu")] private ListBox _navigationMenu;
    
    // Raw storage
    private ListStore _listModel;                                     
    
    // Filtering
    private CustomFilter _filter;                                     
    private FilterListModel _filteredListModel;                       
    private ArtistsFilter _filterState;
    private SimpleAction _filterAction;    
    
    // Sorting
    private CustomSorter _sorter;                                     
    private SortListModel _sortedListModel;
    
    // Selection
    private SingleSelection _selectionModel;                          
    
    // Presentation
    private readonly Dictionary<Id, ArtistModel> _artistModels = new();
    private SignalListItemFactory _signalListItemFactory;
    
    partial void Initialize()
    {
        InitializeArtistsList();
        InitializeActions();
    }

    public void SetActiveFilter(ArtistsFilter filter)
    {
        _filterState = filter;
        
        var displayName = filter switch
        {
            ArtistsFilter.Artists => "All Artists",
            ArtistsFilter.Featured => "Artists",
            ArtistsFilter.Composers => "Composers",
            ArtistsFilter.Conductors => "Conductors",
            ArtistsFilter.Ensembles => "Ensembles",
            ArtistsFilter.Performers => "Performers",
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

        _filterAction.SetState(Variant.NewString(filter.ToString()));
        _artistsMenuButton.SetLabel(displayName);
        
        // Tell the actual filter our preference has changed
        _filter.Changed(FilterChange.Different);
    }

    public void RefreshArtists(IEnumerable<ArtistInfo> artists)
    {
        _listModel.RemoveAll();
        _artistModels.Clear();
        foreach (var artist in artists)
        {
            var model = ArtistModel.NewForArtistInfo(artist, ArtistNameDisplay.Name);
            _artistModels.Add(artist.Id, model);
            _listModel.Append(model);
        }
        
        _filter.Changed(FilterChange.Different);
        _sorter.Changed(SorterChange.Different);
    }

    private void InitializeArtistsList()
    {
        // Raw data
        _listModel = ListStore.New(ArtistModel.GetGType());
        
        // Filter
        _filter = CustomFilter.New(ShouldFilter);
        _filteredListModel = FilterListModel.New(_listModel, _filter);
        _filterAction = SimpleAction.NewStateful("filter", VariantType.String, Variant.NewString("Artists"));        
        _filterAction.OnChangeState += FilterActionOnOnChangeState;        

        // Sorting
        CompareDataFuncT<ArtistModel> sortArtistModel = SortArtistModel;
        _sorter = CustomSorter.New(sortArtistModel);
        _sortedListModel = SortListModel.New(_filteredListModel, _sorter);
        
        // Selection
        _selectionModel = SingleSelection.New(_sortedListModel);
        _selectionModel.Autoselect = false;
        _selectionModel.CanUnselect = true;

        // Presentation
        _signalListItemFactory = SignalListItemFactory.NewWithProperties([]);
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            ((ListItem)args.Object).SetChild(ArtistListItem.NewWithProperties([]));
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is ArtistModel model && listItem.GetChild() is ArtistListItem widget)
                widget.Update(model);
        };
        _artistsListView.OnActivate += ArtistsListViewOnOnActivate;
        
        _artistsListView.SetFactory(_signalListItemFactory);
        _artistsListView.SetModel(_selectionModel);
    }

    private void ArtistsListViewOnOnActivate(ListView sender, ListView.ActivateSignalArgs args)
    {
        if (_selectionModel.SelectedItem is not ArtistModel artistModel) return;
        
        _navigationMenu.UnselectAll();
        
        var artistInfo = artistModel.Artist;        
        var parameter = Variant.NewString(artistInfo.Id.ToString());
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}", parameter);        
    }

    private static int SortArtistModel(ArtistModel a, ArtistModel b)
    {
        var aKey = a.Artist.NameSort ?? a.Artist.Name;
        var bKey = b.Artist.NameSort ?? b.Artist.Name;

        var comparison = string.Compare(aKey, bKey, StringComparison.OrdinalIgnoreCase);
        return comparison switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => 0
        };
    }

    private bool ShouldFilter(Object item)
    {
        if (item is not ArtistModel model) return false;

        var roles = model.Artist.Roles;

        return _filterState switch
        {
            ArtistsFilter.Artists => true,
            ArtistsFilter.Featured => model.IsFeatured,
            ArtistsFilter.Composers => roles.HasFlag(ArtistRoles.Composer),
            ArtistsFilter.Conductors => roles.HasFlag(ArtistRoles.Conductor),
            ArtistsFilter.Ensembles => roles.HasFlag(ArtistRoles.Ensemble),
            ArtistsFilter.Performers => roles.HasFlag(ArtistRoles.Performer),
            _ => true
        };
    }
    
    private void InitializeActions()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_filterAction);
        InsertActionGroup("artists", actionGroup);
        _artistsMenuButton.InsertActionGroup("artists", actionGroup);
    }

    private void FilterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var filter = Enum.TryParse<ArtistsFilter>(value, out var parsed)
            ? parsed
            : ArtistsFilter.Featured;
        SetActiveFilter(filter);
    }
    
    public void Unselect()
    {
        _selectionModel.UnselectAll();
    }

    public void SelectArtist(Id artistId)
    {
        if (!_artistModels.TryGetValue(artistId, out var model))
        {
            _selectionModel.UnselectAll();
            return;
        }

        if (!_listModel.Find(model, out var position))
        {
            // I return now. However, it would be nicer,
            // if we have a sort and filter decorator here.
            // Also, because when I change the filter I don't want to lose the selection
            _selectionModel.UnselectAll();
            return;
        }

        _selectionModel.SelectItem(position, true);
        _artistsListView.ScrollTo(position, ListScrollFlags.None, null);
    }
}