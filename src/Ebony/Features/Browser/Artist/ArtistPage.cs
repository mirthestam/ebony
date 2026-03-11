using Adw;
using Ebony.Core.Library;
using Ebony.Features.Browser.Shared;
using Ebony.Infrastructure;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Ebony.Features.Browser.Artist;

[Subclass<NavigationPage>]
[Template<AssemblyResource>($"ui/{nameof(ArtistPage)}.ui")]
public partial class ArtistPage
{
    public enum ArtistPages
    {
        Artist,
        Empty
    }

    private const string EmptyPageName = "empty-stack-page";
    private const string ArtistPageName = "artist-stack-page";

    [Connect("artist-stack")] private Stack _artistStack;
    
    [Connect("albums-grid-view")] private AlbumsGrid _albumsGrid;
    
    private SimpleAction _sorterAction;
    private ArtistInfo _artist;

    partial void Initialize()
    {
        InitializeActions();
        InitializeGridView();
    }

    public void SetActiveSorting(AlbumsGrid.AlbumSorting filter)
    {
        _sorterAction.SetState(Variant.NewString(filter.ToString()));
        _albumsGrid.SortBy(filter);
    }

    public void TogglePage(ArtistPages page)
    {
        var pageName = page switch
        {
            ArtistPages.Artist => ArtistPageName,
            ArtistPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _artistStack.VisibleChildName = pageName;
    }

    public void ShowArtist(ArtistInfo artistInfo, IReadOnlyList<AlbumModel> albumModels)
    {
        _artist = artistInfo;
        SetTitle(artistInfo.Name);
     
        _albumsGrid.RemoveAll();        
        _albumsGrid.Append(albumModels);
    }

    private void InitializeGridView()
    {
        _sorterAction.OnChangeState += SorterActionOnOnChangeState;
        _albumsGrid.AlbumActivated += AlbumsGridOnAlbumActivated;
    }

    private void AlbumsGridOnAlbumActivated(object? sender, AlbumActivatedEventArgs e)
    {
        var parameters = Variant.NewArray(VariantType.String, [
            e.Album.AlbumId.ToVariant(),
            _artist.Id.ToVariant()
        ]);
        
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbumForArtist.Action}", parameters);
    }

    private void SorterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var sorting = Enum.TryParse<AlbumsGrid.AlbumSorting>(value, out var parsed)
            ? parsed
            : AlbumsGrid.AlbumSorting.Title;
        SetActiveSorting(sorting);
    }
}