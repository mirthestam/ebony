using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gdk;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Album;

public partial class AlbumPagePresenter(
    ILogger<AlbumPagePresenter> logger,
    IMessenger messenger,
    IAria aria,
    IAriaControl ariaControl,
    ArtAssetLoader loader) : IPresenter<AlbumPage>
{
    private AlbumInfo? _album;
    private Art? _currentCoverArt;    
    private CancellationTokenSource? _loadCts;

    public void Attach(AlbumPage view)
    {
        View = view;
        View.ShowFullAlbumAction.OnActivate += ShowFullAlbumActionOnOnActivate;
        View.EnqueueTrack.OnActivate += EnqueueTrackOnOnActivate;
    }

    public AlbumPage? View { get; private set; }
    
    public void Reset()
    {
        LogResetting(logger);
        try
        {
            AbortLoading();
            
            _currentCoverArt?.Dispose();
            _currentCoverArt = null;            
        }
        catch (Exception e)
        {
            LogFailedToAbortLoading(logger, e);
        }
    }    
    
    private void AbortLoading()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }    
    
    private void EnqueueTrackOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        if (args.Parameter == null)
        {
            return;
        }
            
        var serializedId = args.Parameter.GetString(out _);
        var trackId = ariaControl.Parse(serializedId);        
        
        var track = _album?.Tracks.FirstOrDefault(t => t.Track.Id == trackId);
        if (track == null)
        {
            LogCouldNotFindTrackById(logger, serializedId);
            return;
        };
        
        _ =  aria.Queue.EnqueueAsync(track.Track, IQueue.DefaultEnqueueAction);
        
        switch (IQueue.DefaultEnqueueAction)
        {
            case EnqueueAction.Replace:
                // The user is very likely to notice that the action has been executed.
                // Therefore, showing a toast is unnecessary.
                break;
            
            case EnqueueAction.EnqueueNext:
                messenger.Send(new ShowToastMessage($"Track '{track.Track.Title}' inserted next queue."));
                break;
            case EnqueueAction.EnqueueEnd:
                messenger.Send(new ShowToastMessage($"Track '{track.Track.Title}' appended to queue."));
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ShowFullAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // If this is invoked, the album was shown partially.
        // Reload the album, but without any filters
        if (_album == null)
        {
            LogAlbumWasNotLoaded(logger);
            return;
        }
        View?.LoadAlbum(_album);
    }
    
    public async Task LoadAsync(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        LogLoadingAlbum(logger, album.Id);
        
        // Always assume the album is out of date, or only partial.
        album = await aria.Library.GetAlbumAsync(album.Id) ?? album;
        
        AbortLoading();
        _loadCts = new CancellationTokenSource();
        var ct= _loadCts.Token;
            
        _album = album;

        try
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.LoadAlbum(album, filteredArtist);
            }, ct);                        
            
            _currentCoverArt?.Dispose();
            _currentCoverArt = null;            
            
            var assetId = album.Assets.FrontCover?.Id ?? Id.Empty;
            var newCoverArt = await loader.LoadFromAssetAsync(assetId, ct);
            ct.ThrowIfCancellationRequested();
            
            if (newCoverArt == null)
            {
                LogCouldNotLoadAlbumCoverForAlbum(album.Id);
                return;
            }
            
            var previousCoverArt = _currentCoverArt;
            _currentCoverArt = newCoverArt;            

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetCoverArt(newCoverArt);
            }, ct);              
            
            previousCoverArt?.Dispose();
        }
        catch (OperationCanceledException)
        {
        }        
        catch (Exception e)
        {
            LogCouldNotLoadAlbumCoverForAlbum(e, album.Id);
        }
    }
    
    [LoggerMessage(LogLevel.Warning, "Could not load album cover for album {albumId}")]
    partial void LogCouldNotLoadAlbumCoverForAlbum(Id albumId);

    [LoggerMessage(LogLevel.Warning, "Could not load album cover for album {albumId}")]
    partial void LogCouldNotLoadAlbumCoverForAlbum(Exception e, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Resetting.")]
    static partial void LogResetting(ILogger<AlbumPagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Enqueueing album {albumId} to queue.")]
    static partial void LogEnqueueingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Playing album {albumId}.")]
    static partial void LogPlayingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Loading album {albumId}")]
    static partial void LogLoadingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Error, "Failed to abort loading.")]
    static partial void LogFailedToAbortLoading(ILogger<AlbumPagePresenter> logger, Exception e);

    [LoggerMessage(LogLevel.Warning, "Could not find track with ID {trackId}")]
    static partial void LogCouldNotFindTrackById(ILogger<AlbumPagePresenter> logger, string trackId);

    [LoggerMessage(LogLevel.Warning, "Album was not loaded.")]
    static partial void LogAlbumWasNotLoaded(ILogger<AlbumPagePresenter> logger);
}