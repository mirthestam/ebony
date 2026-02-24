using Aria.Core.Player;
using Aria.Infrastructure;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Queue;

[Subclass<Stack>]
[Template<AssemblyResource>($"ui/{nameof(Queue)}.ui")]
public partial class Queue
{
    public enum QueuePages
    {
        Tracks,
        Empty
    }

    private const string EmptyPageName = "empty-playlist-page";
    private const string TracksPageName = "playlist-page";

    private bool _initialized;

    [Connect("tracks-list-view")] private ListView _tracksListView;

    private SignalListItemFactory _itemFactory;
    private ListStore _listStore;
    private SingleSelection _selection;

    private uint? _currentTrackIndex;

    [Connect("queue-gesture-click")] private GestureClick _queueGestureClick;
    [Connect("queue-gesture-long-press")] private GestureLongPress _queueGestureLongPress;
    [Connect("queue-popover-menu")] private PopoverMenu _queuePopoverMenu;
    [Connect("track-popover-menu")] private PopoverMenu _trackPopoverMenu;

    public event EventHandler<EnqueueRequestedEventArgs> EnqueueRequested;
    public event EventHandler<MoveRequestedEventArgs> MoveRequested;
    public event EventHandler<TrackActivatedEventArgs>? TrackActivated;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(QueuePages.Empty);

        InitializeListView();
        InitializeQueueActionGroup();
    }

    private void InitializeListView()
    {
        _itemFactory = SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;
        _itemFactory.OnTeardown += ItemFactoryOnOnTeardown;

        _listStore = ListStore.New(QueueTrackModel.GetGType());
        _selection = SingleSelection.New(_listStore);
        _selection.CanUnselect = true;
        _selection.Autoselect = false;
        _tracksListView.SetFactory(_itemFactory);
        _tracksListView.SetModel(_selection);

        _tracksListView.OnActivate += TracksListViewOnOnActivate;
        
        _queueGestureClick.OnReleased += GridQueueGestureClickOnReleased;
        _queueGestureLongPress.OnPressed += GridQueueGestureLongPressOnPressed; 
    }
    
    public void TogglePage(QueuePages page)
    {
        var pageName = page switch
        {
            QueuePages.Tracks => TracksPageName,
            QueuePages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        SetVisibleChildName(pageName);
    }

    public void CurrentTrackIndex(uint? index, PlaybackState state)
    {
        if (index == Gtk.Constants.INVALID_LIST_POSITION)
        {
            index = null;
        }
        
        // Need to find the current playing track in the ListStore.
        if (_currentTrackIndex.HasValue)
        {
            if (_currentTrackIndex != Gtk.Constants.INVALID_LIST_POSITION)
            {
                if (_listStore.GetObject(_currentTrackIndex.Value) is QueueTrackModel previousTrack)
                {
                    previousTrack.Playing = PlaybackState.Unknown;
                }
            }
        }

        // Set the new playing track
        _currentTrackIndex = index;

        ChangePlayState(state);

        if (!_currentTrackIndex.HasValue) return;
        _tracksListView.ScrollTo(_currentTrackIndex.Value, ListScrollFlags.Focus, null);
    }

    public void ChangePlayState(PlaybackState state)
    {
        if (!_currentTrackIndex.HasValue) return;
        if (_listStore.GetObject(_currentTrackIndex.Value) is not QueueTrackModel track) return;

        // Need PlaybackState here from the player
        track.Playing = state;
    }

    public void RefreshTracks(IEnumerable<QueueTrackModel> tracks)
    {
        // We assume the caller (presenter) reuses QueueTrackModel instances where possible.
        UpdateTracks(tracks as IReadOnlyList<QueueTrackModel> ?? tracks.ToList());
    }

    private void UpdateTracks(IReadOnlyList<QueueTrackModel> desired)
    {
        var desiredCount = desired.Count;

        for (var i = 0; i < desiredCount; i++)
        {
            var desiredItem = desired[i];

            var currentCount = (int)_listStore.GetNItems();
            if (i < currentCount)
            {
                var currentItemObj = _listStore.GetItem((uint)i);
                if (ReferenceEquals(currentItemObj, desiredItem))
                    continue;

                // Look for the desired item later in the store (move case)
                var foundIndex = -1;
                for (var j = i + 1; j < currentCount; j++)
                {
                    var obj = _listStore.GetItem((uint)j);
                    if (ReferenceEquals(obj, desiredItem))
                    {
                        foundIndex = j;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    // Move: remove at foundIndex, insert at i
                    _listStore.Remove((uint)foundIndex);
                    _listStore.Insert((uint)i, desiredItem);
                }
                else
                {
                    // Insert: new item at i
                    _listStore.Insert((uint)i, desiredItem);
                }
            }
            else
            {
                // Append remaining new items
                _listStore.Append(desiredItem);
            }
        }

        // Remove any extra items at the end
        for (var i = (int)_listStore.GetNItems() - 1; i >= desiredCount; i--)
        {
            _listStore.Remove((uint)i);
        }
    }

    private void OnItemFactoryOnOnSetup(SignalListItemFactory _, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = TrackListItem.NewWithProperties([]);

        // Gestures
        child.GestureClick.OnReleased += TrackGestureClickOnOnReleased;
        child.GestureLongPress.OnPressed += TrackGestureLongPressOnOnPressed;

        // Drag source
        SetupDragDropForItem(child);

        item.SetChild(child);
    }

    private void ItemFactoryOnOnTeardown(SignalListItemFactory sender, SignalListItemFactory.TeardownSignalArgs args)
    {
        var item = (ListItem)args.Object;
        if (item.Child is not TrackListItem child) return;

        child.GestureClick.OnReleased -= TrackGestureClickOnOnReleased;
        child.GestureLongPress.OnPressed -= TrackGestureLongPressOnOnPressed;
    }

    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (QueueTrackModel)listItem.GetItem()!;
        var widget = (TrackListItem)listItem.GetChild()!;
        widget.Bind(modelItem);
    }
}