using Ebony.Backends.MPD.Connection.Commands;
using Ebony.Backends.MPD.Connection.Commands.Playlist;
using Ebony.Backends.MPD.Extraction;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using MpcNET.Commands.Playlist;
using ListPlaylistInfoCommand = Ebony.Backends.MPD.Connection.Commands.Playlist.ListPlaylistInfoCommand;

namespace Ebony.Backends.MPD;

public partial class Library
{
    // Playlists
    public override async Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        var playlists = await FetchPlaylistsMetaAsync(scope).ConfigureAwait(false);
        if (playlists.Count == 0) return [];
        
        var result = new List<PlaylistInfo>(playlists.Count);
        foreach (var playlist in playlists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Load tracks for the playlists
            var tracks = await FetchPlaylistTracksAsync(scope, playlist.Name).ConfigureAwait(false);
            result.Add(playlist with
            {
                Tracks = tracks.ToList()
            });
        }

        return result;
    }

    public override async Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        var typedPlaylistId = (PlaylistId)playlistId;

        // Find the meta from the meta-headers
        var playlists = await FetchPlaylistsMetaAsync(scope).ConfigureAwait(false);
        var playlist = playlists.FirstOrDefault(x => x.Id == typedPlaylistId);
        if (playlist == null) return null;

        var tracks = await FetchPlaylistTracksAsync(scope, typedPlaylistId.Value).ConfigureAwait(false);

        return playlist with
        {
            Tracks = tracks.ToList()
        };
    }

    public override async Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default)
    {
        var typedPlaylistId = (PlaylistId)id;
        await client.SendCommandAsync(new RmCommand(typedPlaylistId.Value), token: cancellationToken);
    }

    public override async Task RenamePlaylistAsync(Id id, string newName, CancellationToken cancellationToken = default)
    {
        var typedPlaylistId = (PlaylistId)id;
        var result = await client.SendCommandAsync(new RenameCommand(typedPlaylistId.Value, newName), token: cancellationToken);
        if (!result.IsSuccess)
        {
            // now what
        }
    }

    private static async Task<List<PlaylistInfo>> FetchPlaylistsMetaAsync(Connection.ConnectionScope scope)
    {
        var response = await scope.SendCommandAsync(new ListPlaylistsCommand()).ConfigureAwait(false);
        if (!response.IsSuccess) return [];

        return response.Content!.Select(playlist => new PlaylistInfo
        {
            Name = playlist.Name,
            LastModified = playlist.LastModified,
            Id = new PlaylistId(playlist.Name)
        }).ToList();
    }

    private async Task<IEnumerable<AlbumTrackInfo>> FetchPlaylistTracksAsync(Connection.ConnectionScope scope, string playlistName)
    {
        var response = await scope.SendCommandAsync(new ListPlaylistInfoCommand(playlistName)).ConfigureAwait(false);
        if (!response.IsSuccess) return [];

        var tags = response.Content!.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
        return mpdTagParser.ParsePlaylist(tags);
    }
}