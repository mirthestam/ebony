using Adw;
using Aria.Features.Browser.Shared;
using Aria.Infrastructure;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Albums;

[Subclass<NavigationPage>]
[Template<AssemblyResource>($"ui/{nameof(AlbumsPage)}.ui")]
public partial class AlbumsPage
{
    [Connect("albums-grid-view")] private AlbumsGrid _gridView;
    [Connect("artist-stack")] private Stack _artistStack;
    
    // Sorting
    private SimpleAction _sorterAction;

    partial void Initialize()
    {
        InitializeActions();
        InitializeGridView();
    }

    public void ShowAlbums(IReadOnlyList<AlbumModel> models)
    {
        _gridView.RemoveAll();
        _gridView.Append(models);
    }

    private void InitializeGridView()
    {
        _sorterAction.OnChangeState += SorterActionOnOnChangeState;
        _gridView.AlbumActivated += GridViewOnAlbumActivated;
    }

    private void GridViewOnAlbumActivated(object? sender, AlbumActivatedEventArgs e)
    {
        var parameter = e.Album.AlbumId.ToVariant();
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", parameter);
    }

    private void SorterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var sorting = Enum.TryParse<AlbumsGrid.AlbumSorting>(value, out var parsed)
            ? parsed
            : AlbumsGrid.AlbumSorting.Title;
        SetActiveSorting(sorting);
    }
    
    public void SetActiveSorting(AlbumsGrid.AlbumSorting filter)
    {
        _sorterAction.SetState(Variant.NewString(filter.ToString()));

        // Tell the actual sorter our preference has changed
        _gridView.SortBy(filter);
    }
}