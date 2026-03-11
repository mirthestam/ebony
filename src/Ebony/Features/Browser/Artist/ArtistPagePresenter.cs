using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Features.Browser.Shared;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Ebony.Features.Browser.Artist;

public partial class ArtistPagePresenter(
    ILogger<ArtistPagePresenter> logger,
    IMessenger messenger,
    IEbony Ebony,
    ArtAssetLoader artLoader)
{
    private CancellationTokenSource? _loadArtistCancellationTokenSource;

    private ArtistPage? _view;

    public void Attach(ArtistPage view)
    {
        _view = view;
        _view.TogglePage(ArtistPage.ArtistPages.Empty);
    }    
    
    public async Task ResetAsync()
    {
        LogResettingArtistPage();
        
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            _view?.TogglePage(ArtistPage.ArtistPages.Empty);
            _view?.SetTitle("Artist"); // TODO now this name is defined in 2 places
        }).ConfigureAwait(false);                
    }    
    
    public async Task LoadArtistAsync(Id artistId, CancellationToken externalCancellationToken = default)
    {
        LogLoadingArtist(artistId);
        
        await (_loadArtistCancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        _loadArtistCancellationTokenSource?.Dispose();
        _loadArtistCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);

        var ct = _loadArtistCancellationTokenSource.Token;
        
        try
        {
            var artist = await Ebony.Library.GetArtistAsync(artistId, ct);
            if (artist == null) throw new InvalidOperationException("Artist not found");

            var albums = (await Ebony.Library.GetAlbumsAsync(artistId, ct)).ToList();
            var albumModels = albums.Select(AlbumModel.NewForAlbum)
                .OrderBy(a => a.Title)
                .ToList();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                _view?.TogglePage(albums.Count == 0 ? ArtistPage.ArtistPages.Empty : ArtistPage.ArtistPages.Artist);
                _view?.ShowArtist(artist, albumModels);
            }, ct);            

            LogArtistLoadedLoadingArtwork(artistId);
            
            foreach (var album in albumModels) _ = LoadArtForModelAsync(album, ct);
            
            LogArtistArtworkLoaded(artistId);
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            LogCouldNotLoadArtist(e, artistId);
            messenger.Send(new ShowToastMessage("Could not load this artist"));
        }
    }

    private async Task LoadArtForModelAsync(AlbumModel model, CancellationToken ct)
    {
        var artId = model.CoverArtId;
        if (artId == null) return;

        try
        {
            model.CoverArt = await artLoader.LoadFromAssetAsync(artId, ct);
        }
        catch (Exception e)
        {
            LogCouldNotLoadAlbumArtForAlbumId(e, model.AlbumId);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load artist {artistId}")]
    partial void LogCouldNotLoadArtist(Exception e, Id artistId);

    [LoggerMessage(LogLevel.Warning, "Could not load album art for {albumId}")]
    partial void LogCouldNotLoadAlbumArtForAlbumId(Exception e, Id albumId);


    [LoggerMessage(LogLevel.Debug, "Resetting artist page")]
    partial void LogResettingArtistPage();

    [LoggerMessage(LogLevel.Debug, "Artist selection changed: {artistId}")]
    partial void LogArtistSelectionChanged(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Loading artist {artistId}")]
    partial void LogLoadingArtist(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Artist {artistId} loaded. Loading artwork.")]
    partial void LogArtistLoadedLoadingArtwork(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Artist {artistId} artwork loaded.")]
    partial void LogArtistArtworkLoaded(Id artistId);
}