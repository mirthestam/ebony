using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure.Connection;
using Aria.Infrastructure.Inspection;
using Microsoft.Extensions.Logging;
using MpcNET.Commands.Status;

namespace Aria.Backends.MPD.Connection;

/// <summary>
/// The MPD implementation of a backend connection
/// </summary>
public partial class BackendConnection(
    ILogger<BackendConnection> logger,
    Player player,
    Queue queue,
    Client client,
    Library library,
    IIdProvider idProvider,
    ConnectionContext connectionContext,
    IConnectionProfileProvider connectionProfileProvider)
    : BaseBackendConnection( player, queue, library, idProvider)
{
    public void SetCredentials(ConnectionConfig connectionConfig)
    {
        client.Config = connectionConfig;
    }
    
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        // Defensive: make ConnectAsync idempotent (no handler stacking on reconnect)
        UnbindClientEvents();
        BindClientEvents();

        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
        
        // We are connected. Run the stats command.
        var stats = await client.SendCommandAsync(new StatsCommand(), token: cancellationToken);
        
        if (!stats.IsSuccess)
        {
            logger.LogWarning("Could not get stats from MPD");
        }
        else
        {
            var lastUpdatedTimestamp = stats.Content?["db_update"];
            if (lastUpdatedTimestamp == null)
            {
                logger.LogWarning("Could not get last updated timestamp from MPD");
            }
            else
            {
                if (long.TryParse(lastUpdatedTimestamp, out var serverTimestamp))
                {
                    var profile = connectionContext.Profile as ConnectionProfile;
                    var storedTimestamp = profile?.LastDbUpdate ?? 0;
                    
                    if (serverTimestamp > storedTimestamp)
                    {
                        logger.LogInformation("Database was updated while app was not running. Invalidating cache.");
                        library.ServerUpdated(LibraryChangedFlags.Tracks | LibraryChangedFlags.Albums | LibraryChangedFlags.Artists);
                    }
                    
                    if (profile != null)
                    {
                        profile.LastDbUpdate = serverTimestamp;
                        await connectionProfileProvider.SaveProfileAsync(profile);
                    }
                }
            }
        }
    }

    public override async Task DisconnectAsync()
    {
        try
        {
            await client.DisconnectAsync().ConfigureAwait(false);
        }
        finally
        {
            // Always unbind, even if disconnect fails/throws
            UnbindClientEvents();
        }
    }
    
    private void BindClientEvents()
    {
        client.ConnectionChanged += OnClientOnConnectionChanged;
        client.IdleResponseReceived += SessionOnIdleResponseReceived;
        client.StatusChanged += SessionOnStatusChanged;
    }

    private void UnbindClientEvents()
    {
        client.ConnectionChanged -= OnClientOnConnectionChanged;
        client.IdleResponseReceived -= SessionOnIdleResponseReceived;
        client.StatusChanged -= SessionOnStatusChanged;
    }

    private void OnClientOnConnectionChanged(object? a, EventArgs b)
    {
        BackendConnectionState state;
        if (client.IsConnected)
            state = BackendConnectionState.Connected;
        else if (client.IsConnecting)
            state = BackendConnectionState.Connecting;
        else
            state = BackendConnectionState.Disconnected;

        OnConnectionStateChanged(state);
    }

    private async void SessionOnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        try
        {
            await player.UpdateFromStatusAsync(e.Status).ConfigureAwait(false);
            await queue.UpdateFromStatusAsync(e.Status).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogErrorUpdatingPlayerAndPlaylistFromStatus(exception);
        }
    }

    private void SessionOnIdleResponseReceived(object? sender, IdleResponseEventArgs e)
    {
        var subsystems = e.Message;

        if (!subsystems.Contains("playlist") && !subsystems.Contains("player") && !subsystems.Contains("mixer") &&
            !subsystems.Contains("output") && !subsystems.Contains("options") && !subsystems.Contains("update")) return;
        
        var flags = LibraryChangedFlags.None;
        if (subsystems.Contains("playlist"))
        {
            flags |= LibraryChangedFlags.Playlists;
        }

        if (subsystems.Contains("update"))
        {
            // MPD does not have separate updates, we need to assume everything is updated
            flags |= LibraryChangedFlags.Tracks;
            flags |= LibraryChangedFlags.Albums;
            flags |= LibraryChangedFlags.Artists;
        }

        if (flags != LibraryChangedFlags.None)
        {
            library.ServerUpdated(flags);
        }

        _ = client.UpdateStatusAsync(ConnectionType.Idle);
    }

    [LoggerMessage(LogLevel.Error, "Error updating player and playlist from status")]
    partial void LogErrorUpdatingPlayerAndPlaylistFromStatus(Exception e);
}