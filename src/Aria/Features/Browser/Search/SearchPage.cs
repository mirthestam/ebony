using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Search;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(SearchPage)}.ui")]
public partial class SearchPage
{
    private const string ResultsStackPageName = "results-stack-page";
    private const string NoResultsStackPageName = "no-results-stack-page";

    [Connect(NoResultsStackPageName)] private StackPage _noResultsStackPage;
    [Connect(ResultsStackPageName)] private StackPage _resultsStackPage;

    [Connect("search-entry")] private SearchEntry _searchEntry;
    [Connect("search-stack")] private Stack _searchStack;

    [Connect("album-box")] private Box _albumBox;
    [Connect("album-list-box")] private ListBox _albumListBox;

    [Connect("artist-box")] private Box _artistBox;
    [Connect("artist-list-box")] private ListBox _artistListBox;

    [Connect("track-box")] private Box _trackBox;
    [Connect("track-list-box")] private ListBox _trackListBox;

    [Connect("work-box")] private Box _workBox;
    [Connect("work-list-box")] private ListBox _workListBox;

    public event EventHandler<string>? SearchChanged;

    partial void Initialize()
    {
        OnMap += OnOnMap;

        _searchEntry.OnSearchChanged += SearchEntryOnOnSearchChanged;
    }

    private async void OnOnMap(Widget widget, EventArgs eventArgs)
    {
        try
        {
            await GtkDispatch.InvokeIdleAsync(() => { _searchEntry.GrabFocus(); });
        }
        catch
        {
            // OK
        }
    }

    public void Clear()
    {
        ClearDragDrop();

        _artistListBox.RemoveAll();
        _albumListBox.RemoveAll();
        _workListBox.RemoveAll();
        _trackListBox.RemoveAll();
    }

    public void ShowResults(SearchResults results)
    {
        Clear();

        var totalCount = results.Artists.Count + results.Albums.Count + results.Tracks.Count;
        _searchStack.VisibleChildName = totalCount == 0 ? NoResultsStackPageName : ResultsStackPageName;

        _artistBox.Visible = results.Artists.Count > 0;
        _albumBox.Visible = results.Albums.Count > 0;
        _trackBox.Visible = results.Tracks.Count > 0;
        _workBox.Visible = false;

        foreach (var artist in results.Artists.OrderBy(a => a.Name))
        {
            var row = CreateArtistRow(artist);
            _artistListBox.Append(row);
        }

        foreach (var album in results.Albums.OrderBy(a => a.Title))
        {
            var row = CreateAlbumRow(album);
            _albumListBox.Append(row);
        }

        foreach (var track in results.Tracks.OrderBy(t => t.Title))
        {
            var row = CreateTrackRow(track);
            _trackListBox.Append(row);
        }
    }

    private SearchTrackActionRow CreateTrackRow(TrackInfo track)
    {
        // Appearance
        var row = SearchTrackActionRow.NewFor(track.Id);
        row.Activatable = true;
        row.UseMarkup = false;
        row.Title = track.Title;
        row.Subtitle = string.Join(", ", track.CreditsInfo.AlbumArtists.Select(a => a.Artist.Name));

        // Drag & Drop support
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnPrepare += TrackOnPrepare;
        row.AddController(dragSource);
        _trackDragSources.Add(dragSource);

        // Action
        row.ActionName = $"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}";
        var param = Variant.NewArray(VariantType.String, [Variant.NewString(track.Id.ToString())]);
        row.SetActionTargetValue(param);
        return row;
    }

    private SearchAlbumActionRow CreateAlbumRow(AlbumInfo album)
    {
        var row = SearchAlbumActionRow.NewWith(album.Id);

        // Appearance
        row.Activatable = true;
        row.UseMarkup = false;
        row.Title = album.Title;
        row.Subtitle = string.Join(", ", album.CreditsInfo.AlbumArtists.Select(a => a.Artist.Name));

        var image = Image.NewFromIconName("go-next-symbolic");
        row.AddSuffix(image);

        // Drag & Drop support
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnDragBegin += AlbumOnOnDragBegin;
        dragSource.OnPrepare += AlbumDragOnPrepare;
        row.AddController(dragSource);
        _albumDragSources.Add(dragSource);

        // Action
        var albumIdString = album.Id.ToString();
        row.ActionName = $"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}";
        var param = Variant.NewString(albumIdString);
        row.SetActionTargetValue(param);

        return row;
    }

    private void SearchEntryOnOnSearchChanged(SearchEntry sender, EventArgs args)
    {
        SearchChanged?.Invoke(this, _searchEntry.GetText());
    }

    private static ActionRow CreateArtistRow(ArtistInfo artist)
    {
        var row = ActionRow.New();
        row.Activatable = true;
        row.UseMarkup = false;
        row.Title = artist.Name;

        var image = Image.NewFromIconName("go-next-symbolic");
        row.AddSuffix(image);

        row.ActionName = $"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}";
        var param = Variant.NewString(artist.Id.ToString());
        row.SetActionTargetValue(param);
        row.Subtitle = RolesFormatting.Format(artist.Roles);

        return row;
    }
}