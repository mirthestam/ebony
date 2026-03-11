using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Features.Browser.Shared;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Ebony.Features.Browser.Albums;

public partial class AlbumsPagePresenter : IRecipient<LibraryUpdatedMessage>
{
    private readonly ILogger<AlbumsPagePresenter> _logger;
    private readonly IEbony _ebony;
    private readonly ArtAssetLoader _loader;

    private CancellationTokenSource? _loadCts;

    public AlbumsPagePresenter(ILogger<AlbumsPagePresenter> logger, IMessenger messenger, IEbony Ebony,
        ArtAssetLoader loader)
    {
        _ebony = Ebony;
        _logger = logger;
        _loader = loader;

        messenger.RegisterAll(this);
    }

    private AlbumsPage? _view;

    public void Attach(AlbumsPage view)
    {
        _view = view;
        _view.SetActiveSorting(AlbumsGrid.AlbumSorting.Title);  // Configurable default in the future?
    }
    
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task Reset()
    {
        LogResetting();
        await AbortAndClear();
    }    
    
    private async Task AbortAndClear()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        await GtkDispatch.InvokeIdleAsync(() =>
            _view?.ShowAlbums([])
        );
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        if (message.Value.HasFlag(LibraryChangedFlags.Albums))
        {
            _ = LoadAsync();
        }
    }

    private async Task LoadAsync(CancellationToken externalCancellationToken = default)
    {
        LogLoadingAlbums();
        await AbortAndClear();
        
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _loadCts.Token;
        
        try
        {
            var albums = await _ebony.Library.GetAlbumsAsync(cancellationToken).ConfigureAwait(false);
            
            var albumModels = albums.Select(AlbumModel.NewForAlbum)
                .OrderBy(a => a.Title)
                .ToList();
            cancellationToken.ThrowIfCancellationRequested();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                _view?.ShowAlbums(albumModels);
            }, cancellationToken).ConfigureAwait(false);
            
            LogAlbumsLoaded();

            await ProcessArtworkAsync(albumModels, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested) LogCouldNotLoadAlbums(e);
        }
    }

    private async Task ProcessArtworkAsync(IEnumerable<AlbumModel> models, CancellationToken ct)
    {
        LogLoadingAlbumsArtwork();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = ct
        };

        try
        {
            await Parallel.ForEachAsync(models, options,
                async (model, token) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await LoadArtForModelAsync(model, token);
                });
            
            LogAlbumsArtworkLoaded();
        }
        catch (OperationCanceledException)
        {
            LogArtworkLoadingAborted();
        }
    }

    private async Task LoadArtForModelAsync(AlbumModel model, CancellationToken ct = default)
    {
        var artId = model.CoverArtId;
        if (artId == null) return;

        try
        {
            model.CoverArt = await _loader.LoadFromAssetAsync(artId, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            LogResourceResourceIdNotFoundInLibrary(e, artId);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load albums")]
    partial void LogCouldNotLoadAlbums(Exception e);

    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceResourceIdNotFoundInLibrary(Exception e, Id resourceId);

    [LoggerMessage(LogLevel.Debug, "Resetting albums page")]
    partial void LogResetting();

    [LoggerMessage(LogLevel.Debug, "Loading albums")]
    partial void LogLoadingAlbums();

    [LoggerMessage(LogLevel.Debug, "Albums loaded.")]
    partial void LogAlbumsLoaded();

    [LoggerMessage(LogLevel.Debug, "Loading albums artwork")]
    partial void LogLoadingAlbumsArtwork();

    [LoggerMessage(LogLevel.Debug, "Albums artwork loaded.")]
    partial void LogAlbumsArtworkLoaded();

    [LoggerMessage(LogLevel.Debug, "Artwork loading aborted.")]
    partial void LogArtworkLoadingAborted();
}