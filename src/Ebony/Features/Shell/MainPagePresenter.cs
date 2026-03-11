using Ebony.Core;
using Ebony.Core.Connection;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Queue;
using Ebony.Features.Browser;
using Ebony.Features.Player;
using Ebony.Features.PlayerBar;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Ebony.Features.Shell;

public partial class MainPagePresenter : IRootPresenter<MainPage>, IRecipient<QueueStateChangedMessage>
{
    private readonly ILogger<MainPagePresenter> _logger;
    private readonly IEbonyControl _ebonyControl;
    private readonly IEbony _ebony;
    private readonly BrowserHostPresenter _browserHostPresenter;
    private readonly PlayerPresenter _playerPresenter;
    private readonly PlayerBarPresenter _playerBarPresenter;
    private readonly ArtAssetLoader _artAssetLoader;
    
    private CancellationTokenSource? _connectionCancellationTokenSource;
    
    private Task _activeTask = Task.CompletedTask;

    public MainPagePresenter(ILogger<MainPagePresenter> logger,
        BrowserHostPresenter browserHostPresenter,
        PlayerPresenter playerPresenter,
        PlayerBarPresenter playerBarPresenter,
        IMessenger messenger, IEbonyControl ebonyControl, IEbony Ebony, ArtAssetLoader artAssetLoader)
    {
        _logger = logger;
        _browserHostPresenter = browserHostPresenter;
        _playerPresenter = playerPresenter;
        _playerBarPresenter = playerBarPresenter;
        _ebonyControl = ebonyControl;
        _ebony = Ebony;
        _artAssetLoader = artAssetLoader;


        _ebonyControl.StateChanged += EbonyControlOnStateChanged;
        
        messenger.RegisterAll(this);
    }
    
    public void Attach(MainPage view, AttachContext context)
    {
        View = view;
        _browserHostPresenter.Attach(view.BrowserHost, context);
        _playerBarPresenter.Attach(view.PlayerBar);
        _playerPresenter.Attach(view.Player, context);
    }
    
    public async void Receive(QueueStateChangedMessage message)
    {
        try
        {
            if (!message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;
            
            Id? assetId;

            switch (_ebony.Queue.Mode)
            {
                case QueueMode.SingleAlbum:
                    var firstTrack = _ebony.Queue.Tracks.FirstOrDefault();
                    assetId = firstTrack?.Track.Assets.FrontCover?.Id;                    
                    break;
                case QueueMode.Playlist:
                    var track = _ebony.Queue.CurrentTrack;
                    assetId = track?.Track.Assets.FrontCover?.Id;                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (assetId == null)
            {
                View?.Colorize(null);
            }
            else
            {
                var art = await _artAssetLoader.LoadFromAssetAsync(assetId);
                View?.Colorize(art);
            }
        }
        catch (Exception e)
        {
            LogFailedToColorizeArtwork(e);           
        }
    }    
    
    public MainPage? View { get; private set; }
    
    private void EbonyControlOnStateChanged(object? sender, EngineStateChangedEventArgs e)
    {
        switch (e.State)
        {
            case EngineState.Ready:
                CancelActiveRefresh();
                _activeTask = SequenceTaskAsync(() => OnEngineReadyAsync());
                break;
            
            case EngineState.Stopped:
                CancelActiveRefresh();
                _activeTask = SequenceTaskAsync(() => OnEngineStoppedAsync());
                break;
            
            case EngineState.Starting:
                _ = OnEngineStartingAsync();
                break;                
        }
    }
    
    private void CancelActiveRefresh()
    {
        if (_connectionCancellationTokenSource == null) return;
        _connectionCancellationTokenSource.Cancel();
        _connectionCancellationTokenSource.Dispose();
        _connectionCancellationTokenSource = null;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogLoadingUiForConnectedBackend();
            
            await _playerPresenter.RefreshAsync(cancellationToken);
            await _playerBarPresenter.RefreshAsync(cancellationToken);
            await _browserHostPresenter.RefreshAsync(cancellationToken);
            
            _logger.LogInformation(cancellationToken.IsCancellationRequested
                ? "UI refresh was cancelled before completion."
                : "UI succesfully refreshed.");
        }
        catch (OperationCanceledException)
        {
            LogUiRefreshAborted();
        }
        finally
        {
            if (_connectionCancellationTokenSource?.Token == cancellationToken)
            {
                _connectionCancellationTokenSource.Dispose();
                _connectionCancellationTokenSource = null;
            }
        }    
    }

    private async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogDisconnectedFromBackendUnloadingUi();
            await _browserHostPresenter.ResetAsync(cancellationToken);
            await _playerPresenter.ResetAsync();
            await _playerBarPresenter.ResetAsync();
            LogUiIsReset();
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            LogFailedToResetUi(e);
        }        
    }
    
    private async Task OnEngineReadyAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken);
    }
    
    private async Task OnEngineStartingAsync()
    {
        await Task.CompletedTask;
    }

    private async Task OnEngineStoppedAsync(CancellationToken cancellationToken = default)
    {
        await ResetAsync(cancellationToken);
    }

    private async Task SequenceTaskAsync(Func<Task> action)
    {
        try
        {
            await _activeTask;
        }
        catch(Exception e)
        {
            LogSequenceTaskAbortedDueToException(e);
        }

        await action();
    }
    
    [LoggerMessage(LogLevel.Information, "Backend connected - Connecting UI.")]
    partial void LogLoadingUiForConnectedBackend();

    [LoggerMessage(LogLevel.Information, "backend disconnected - Disconnecting UI.")]
    partial void LogDisconnectedFromBackendUnloadingUi();

    [LoggerMessage(LogLevel.Warning, "New connection established. Aborting current UI refresh.")]
    partial void LogNewConnectionEstablishedAbortingCurrentUiRefresh();

    [LoggerMessage(LogLevel.Information, "Connection lost during UI refresh. Aborting UI refresh.")]
    partial void LogConnectionLostDuringUiRefreshAbortingUiRefresh();

    [LoggerMessage(LogLevel.Information, "UI refresh aborted.")]
    partial void LogUiRefreshAborted();

    [LoggerMessage(LogLevel.Information, "UI is reset.")]
    partial void LogUiIsReset();

    [LoggerMessage(LogLevel.Error, "Failed to reset UI.")]
    partial void LogFailedToResetUi(Exception e);

    [LoggerMessage(LogLevel.Warning, "Sequence task aborted due to exception.")]
    partial void LogSequenceTaskAbortedDueToException(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to colorize artwork")]
    partial void LogFailedToColorizeArtwork(Exception e);
}