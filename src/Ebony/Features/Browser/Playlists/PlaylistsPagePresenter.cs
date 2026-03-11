using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Ebony.Features.Browser.Playlists;

public partial class PlaylistsPagePresenter : IRootPresenter<PlaylistsPage>, IRecipient<LibraryUpdatedMessage>
{
    private CancellationTokenSource? _loadCts;
    private readonly ILogger<PlaylistsPagePresenter> _logger;
    private readonly IEbony _ebony;
    private readonly ArtAssetLoader _loader;
    private readonly IPlaylistNameValidator _playlistNameValidator;

    public PlaylistsPagePresenter(ILogger<PlaylistsPagePresenter> logger,
        IEbony Ebony,
        ArtAssetLoader loader,
        IPlaylistNameValidator playlistNameValidator,
        IMessenger messenger)
    {
        _logger = logger;
        _ebony = Ebony;
        _loader = loader;
        _playlistNameValidator = playlistNameValidator;
        
        messenger.RegisterAll(this);
    }

    public void Attach(PlaylistsPage view, AttachContext context)
    {
        View = view;
        View.NameValidator = _playlistNameValidator;
        View.DeleteRequested += ViewOnDeleteRequested;
        View.RenameRequested += ViewOnRenameRequested;
        View.TogglePage(PlaylistsPage.PlaylistsPages.Empty);
    }

    private async void ViewOnRenameRequested(object? sender, RenamePlaylistEventArgs e)
    {
        try
        {
            await _ebony.Library.RenamePlaylistAsync(e.PlaylistId, e.PlaylistName);
        }
        catch (Exception ex)
        {
            LogFailedToRenamePlaylist(ex);
        }
    }

    private async void ViewOnDeleteRequested(object? sender, Id e)
    {
        try
        {
            await _ebony.Library.DeletePlaylistAsync(e);
        }
        catch (Exception ex)
        {
            LogFailedToDeletePlaylist(ex);
        }
    }

    public PlaylistsPage? View { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }

    public async void Receive(LibraryUpdatedMessage message)
    {
        try
        {
            if (message.Value.HasFlag(LibraryChangedFlags.Playlists))
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            LogFailedToHandleLibraryUpdatedMessage(ex);
        }
    }    
    
    private void AbortAndClear()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        View?.ShowPlaylists([]);
    }

    private async Task LoadAsync(CancellationToken externalCancellationToken = default)
    {
        AbortAndClear();

        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _loadCts.Token;

        try
        {
            var infos = await _ebony.Library.GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);

            var models = infos.Select(PlaylistModel.NewForPlaylistInfo)
                .OrderBy(a => a.Name)
                .ToList();
            cancellationToken.ThrowIfCancellationRequested();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                View?.TogglePage(models.Count == 0
                    ? PlaylistsPage.PlaylistsPages.Empty
                    : PlaylistsPage.PlaylistsPages.Playlists);
                View?.ShowPlaylists(models);
            }, cancellationToken).ConfigureAwait(false);

            await ProcessArtworkAsync(models, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) 
        {
            LogFailedToLoadPlaylists(ex);
        }
    }

    private async Task ProcessArtworkAsync(IEnumerable<PlaylistModel> models, CancellationToken ct)
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

    private async Task LoadArtForModelAsync(PlaylistModel model, CancellationToken ct = default)
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

    public async Task ResetAsync()
    {
        await GtkDispatch.InvokeIdleAsync(() => { View?.TogglePage(PlaylistsPage.PlaylistsPages.Empty); });
    }

    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceResourceIdNotFoundInLibrary(Exception e, Id resourceId);

    [LoggerMessage(LogLevel.Debug, "Loading albums artwork")]
    partial void LogLoadingAlbumsArtwork();

    [LoggerMessage(LogLevel.Debug, "Albums artwork loaded.")]
    partial void LogAlbumsArtworkLoaded();

    [LoggerMessage(LogLevel.Debug, "Artwork loading aborted.")]
    partial void LogArtworkLoadingAborted();

    [LoggerMessage(LogLevel.Error, "Failed to rename playlist")]
    partial void LogFailedToRenamePlaylist(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to delete playlist")]
    partial void LogFailedToDeletePlaylist(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to handle library updated message")]
    partial void LogFailedToHandleLibraryUpdatedMessage(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to load playlists")]
    partial void LogFailedToLoadPlaylists(Exception ex);
}