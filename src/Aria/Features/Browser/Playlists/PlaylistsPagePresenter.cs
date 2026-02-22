using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Playlists;

public partial class PlaylistsPagePresenter : IRootPresenter<PlaylistsPage>, IRecipient<LibraryUpdatedMessage>
{
    private CancellationTokenSource? _loadCts;
    private readonly ILogger<PlaylistsPagePresenter> _logger;
    private readonly IAria _aria;
    private readonly ArtAssetLoader _loader;
    private readonly IPlaylistNameValidator _playlistNameValidator;

    public PlaylistsPagePresenter(ILogger<PlaylistsPagePresenter> logger,
        IAria aria,
        ArtAssetLoader loader,
        IPlaylistNameValidator playlistNameValidator,
        IMessenger messenger)
    {
        _logger = logger;
        _aria = aria;
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
            await _aria.Library.RenamePlaylistAsync(e.PlaylistId, e.PlaylistName);
        }
        catch
        {
            // OK
        }
    }

    private async void ViewOnDeleteRequested(object? sender, Id e)
    {
        try
        {
            await _aria.Library.DeletePlaylistAsync(e);
        }
        catch
        {
            // OK
        }
    }

    public PlaylistsPage? View { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
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
            var infos = await _aria.Library.GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);

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
        catch 
        {
            // OK
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
            // Ok
        }
        catch (Exception e)
        {
            LogResourceResourceIdNotFoundInLibrary(e, artId);
        }
    }

    public async Task ResetAsync()
    {
        //LogResettingArtistPage();

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

    public async void Receive(LibraryUpdatedMessage message)
    {
        try
        {
            if (message.Value.HasFlag(LibraryChangedFlags.Playlists))
            {
                await RefreshAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}