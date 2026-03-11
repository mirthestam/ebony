using Ebony.Backends.MPD.Connection;
using Ebony.Backends.MPD.Connection.Commands.Queue;
using Ebony.Backends.MPD.Extraction;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using ListPlaylistInfoCommand = Ebony.Backends.MPD.Connection.Commands.Playlist.ListPlaylistInfoCommand;
using PlaylistInfoCommand = Ebony.Backends.MPD.Connection.Commands.Playlist.PlaylistInfoCommand;
using SaveCommand = Ebony.Backends.MPD.Connection.Commands.Playlist.SaveCommand;

namespace Ebony.Backends.MPD;

public partial class Queue(Client client, ITagParser parser, MPDTagParser mpdTagParser, ILogger<Queue> logger) : BaseQueue
{
    private readonly List<QueueTrackInfo> _tracksList = [];

    public override IEnumerable<QueueTrackInfo> Tracks => _tracksList;

    private async Task RefreshTracksAsync()
    {
        var (isSuccess, tagPairs) = await client.SendCommandAsync(new PlaylistInfoCommand()).ConfigureAwait(false);
        if (!isSuccess) throw new InvalidOperationException("Failed to get playlist info");
        if (tagPairs == null) throw new InvalidOperationException("No playlist info found");

        var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();

        var tracks = mpdTagParser.ParseQueue(tags);
        _tracksList.Clear();
        _tracksList.AddRange(tracks);
        
        var mode = QueueMode.Playlist;
        var firstTrack = _tracksList.FirstOrDefault();
        if (firstTrack != null && _tracksList.All(t => t.Track.AlbumId == firstTrack.Track.AlbumId))
        {
            mode = QueueMode.SingleAlbum;
        }

        _mode = mode;        
    }

    public override Task EnqueueAsync(Info info, EnqueueAction action)
    {
        return EnqueueAsync([info], action);
    }

    public override async Task EnqueueAsync(IEnumerable<Info> items, EnqueueAction action)
    {
        // in MPD, we enqueue per track. Therefore, let's expand our items.

        var tracks = new List<TrackInfo>();
        foreach (var info in items)
        {
            switch (info)
            {
                case AlbumTrackInfo albumTrack:
                    tracks.Add(albumTrack.Track);
                    break;

                case TrackInfo track:
                    tracks.Add(track);
                    break;

                case AlbumInfo album:
                    tracks.AddRange(album.Tracks.Select(t => t.Track));
                    break;

                case PlaylistInfo playlist:
                    tracks.AddRange(playlist.Tracks.Select(t => t.Track));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(items), items, null);
            }
        }

        await EnqueueAsync(tracks, action).ConfigureAwait(false);
    }

    public override async Task EnqueueAsync(Info info, uint index)
    {
        switch (info)
        {
            case AlbumTrackInfo albumTrack:
                await EnqueueAsync([albumTrack.Track], (int)index).ConfigureAwait(false);
                break;

            case TrackInfo track:
                await EnqueueAsync([track], (int)index).ConfigureAwait(false);
                break;

            case AlbumInfo album:
                await EnqueueAsync(album.Tracks.Select(t => t.Track), (int)index).ConfigureAwait(false);
                break;

            case PlaylistInfo playlist:
                await EnqueueAsync(playlist.Tracks.Select(t => t.Track), (int)index).ConfigureAwait(false);
                break;
        }
    }

    public override async Task MoveAsync(Id sourceTrackId, uint targetPlaylistIndex)
    {
        try
        {
            var queueTrackId = (QueueTrackId)sourceTrackId;

            // When the target is located after the source in the queue, move it up by one position.
            // MPD seems to handle this by first removing the track, then reinserting it at the new index.
            var sourceTrack = _tracksList.FirstOrDefault(t => t.Id == queueTrackId);
            if (sourceTrack == null) throw new InvalidOperationException("Source track not found");
            if (targetPlaylistIndex > sourceTrack.Position) targetPlaylistIndex--;

            var command = new MoveIdCommand(queueTrackId.Value, (int)targetPlaylistIndex);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToMoveTrack(logger, e);
        }
    }

    public override QueueMode Mode => _mode;

    private QueueMode _mode;
    
    public override async Task ClearAsync()
    {
        try
        {
            await client.SendCommandAsync(new ClearCommand()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToClearQueue(logger, e);
        }
    }

    public override async Task SaveOrAppendToPlaylistAsync(string playlistName)
    {
        try
        {
            var method = SaveCommand.SaveMethod.Create;
            var existsCommand = new ListPlaylistInfoCommand(playlistName);
            var existsResponse = await client.SendCommandAsync(existsCommand).ConfigureAwait(false);
            if (existsResponse.IsSuccess)
            {
                method = SaveCommand.SaveMethod.Replace;
            }

            var command = new SaveCommand(playlistName, method);
            await client.SendCommandAsync(command);
        }
        catch (Exception e)
        {
            LogFailedToSaveQueue(logger, e);
        }
    }

    public override async Task RemoveTrackAsync(Id trackId)
    {
        try
        {
            var queueTrackId = (QueueTrackId)trackId;
            await client.SendCommandAsync(new DeleteIdCommand(queueTrackId.Value)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToRemoveTrack(logger, e);
        }
    }

    public override async Task SetShuffleAsync(bool enabled)
    {
        try
        {
            var command = new RandomCommand(enabled);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToSetShuffle(logger, e);
        }
    }

    public override async Task SetRepeatAsync(RepeatMode repeatMode)
    {
        try
        {
            bool repeat;
            bool single;

            switch (repeatMode)
            {
                case RepeatMode.Disabled:
                    repeat = false;
                    single = false;
                    break;

                case RepeatMode.All:
                    repeat = true;
                    single = false;
                    break;

                case RepeatMode.Single:
                    repeat = true;
                    single = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }

            using var scope = await client.CreateConnectionScopeAsync();

            await scope.SendCommandAsync(new RepeatCommand(repeat)).ConfigureAwait(false);
            await scope.SendCommandAsync(new SingleCommand(single)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToSetRepeat(logger, e);
        }
    }

    public override async Task SetConsumeAsync(bool enabled)
    {
        try
        {
            var command = new ConsumeCommand(enabled);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToSetConsume(logger, e);
        }
    }

    private async Task EnqueueAsync(IEnumerable<TrackInfo> tracks, int index)
    {
        try
        {
            var commandList = new CommandList();
            foreach (var track in tracks.Reverse())
            {
                commandList.Add(new AddCommand(track.FileName, index));
            }

            await client.SendCommandAsync(commandList).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToPlayTracks(logger, e);
        }
    }

    private async Task EnqueueAsync(IEnumerable<TrackInfo> tracks, EnqueueAction action)
    {
        try
        {
            var commandList = new CommandList();


            switch (action)
            {
                case EnqueueAction.Replace:
                    commandList.Add(new ClearCommand());
                    foreach (var track in tracks)
                    {
                        commandList.Add(new AddCommand(Client.Escape(track.FileName!)));
                    }

                    commandList.Add(new PlayCommand(0));
                    break;
                case EnqueueAction.EnqueueNext:
                    foreach (var track in tracks.Reverse())
                    {
                        // Need to escape filenames here.
                        // I should modify the command library to automatically escape all commands
                        commandList.Add(new AddCommand(Client.Escape(track.FileName!),
                            (int)(Order.CurrentIndex + 1 ?? 0)));
                    }

                    break;

                case EnqueueAction.EnqueueEnd:
                    foreach (var track in tracks)
                    {
                        commandList.Add(new AddCommand(Client.Escape(track.FileName!)));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            await client.SendCommandAsync(commandList).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToPlayTracks(logger, e);
        }
    }
    
    public async Task UpdateFromStatusAsync(MpdStatus e)
    {
        // We received new information from MPD. Get all relevant information for this playlist.
        var flags = QueueStateChangedFlags.None;

        // Consume
        if (Consume.Enabled != e.Consume || !Consume.Supported)
        {
            Consume = new ConsumeSettings
            {
                Enabled = e.Consume,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Consume;
        }

        // Shuffle
        if (Shuffle.Enabled != e.Random || !Shuffle.Supported)
        {
            Shuffle = new ShuffleSettings
            {
                Enabled = e.Random,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Shuffle;
        }

        // Repeat
        var newRepeatMode = RepeatMode.Disabled;
        if (e is { Repeat: true, Single: true }) newRepeatMode = RepeatMode.Single;
        newRepeatMode = e.Repeat switch
        {
            true when !e.Single => RepeatMode.All,
            false => RepeatMode.Disabled,
            _ => newRepeatMode
        };

        if (newRepeatMode != Repeat.Mode || !Repeat.Supported)
        {
            Repeat = new RepeatSettings
            {
                Mode = newRepeatMode,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Repeat;
        }

        // Playlist 
        var newPlaylistId = new QueueId(e.Playlist);

        if (Id is not QueueId)
        {
            // This is the first time we are processing, this instance.
            // Therefore, just force an update of the playbackOrder.
            // We will NOT update the ID; as this would case the player to
            // reload the playlist it had loaded at the connection.
            Id = newPlaylistId;
            Length = (uint)e.PlaylistLength;
            flags |= QueueStateChangedFlags.Id;
            flags |= QueueStateChangedFlags.PlaybackOrder;
        }

        // Skip comparison unless a playlist ID is present to prevent loading the playlist unnecessarily from the app.
        else if (Id is not QueueId oldId || newPlaylistId.Value != oldId.Value)
        {
            flags |= QueueStateChangedFlags.Id;
            flags |= QueueStateChangedFlags.PlaybackOrder;

            // // The playback order changed implicitly, even though the current track may still be at the same index.
            // // This will trigger an order change
            // Order = PlaybackOrder.Default;

            Id = newPlaylistId;
            Length = (uint)e.PlaylistLength;
        }

        // Order
        if (Order.CurrentIndex != e.Song)
        {
            Order = new PlaybackOrder
            {
                CurrentIndex = (uint?)e.Song,
                HasNext = e.NextSongId > 0
            };
            flags |= QueueStateChangedFlags.PlaybackOrder;
        }

        if (flags.HasFlag(QueueStateChangedFlags.Id) || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            // Refresh the tracks
            await RefreshTracksAsync();
        }

        if (flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            var (isSuccess, keyValuePairs) =
                await client.SendCommandAsync(new GetCurrentTrackInfoCommand()).ConfigureAwait(false);
            if (!isSuccess) throw new InvalidOperationException("Failed to get current track info");
            if (keyValuePairs == null) throw new InvalidOperationException("No current track info found");

            var tagPairs = keyValuePairs.ToList();

            var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
            if (tagPairs.Count == 0)
            {
                CurrentTrack = null;
            }
            else
            {
                var trackInfo = parser.ParseQueueTrack(tags);
                if (trackInfo.Track.FileName != null)
                {
                    trackInfo = trackInfo with
                    {
                        Track = trackInfo.Track.WithMPDAsset()
                    };
                }

                CurrentTrack = trackInfo;
            }
        }

        if (flags != QueueStateChangedFlags.None)
        {
            OnStateChanged(flags);
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to move track")]
    static partial void LogFailedToMoveTrack(ILogger<Queue> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to clear queue")]
    static partial void LogFailedToClearQueue(ILogger<Queue> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to save queue")]
    static partial void LogFailedToSaveQueue(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to remove track")]
    static partial void LogFailedToRemoveTrack(ILogger<Queue> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set shuffle")]
    static partial void LogFailedToSetShuffle(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set repeat")]
    static partial void LogFailedToSetRepeat(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set consume")]
    static partial void LogFailedToSetConsume(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to play tracks")]
    static partial void LogFailedToPlayTracks(ILogger<Queue> logger, Exception ex);
}