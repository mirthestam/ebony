using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

public class LibraryProxy : ILibrarySource
{
    private ILibrarySource? _innerLibrary;
    private readonly SemaphoreSlim _artistsLock = new(1, 1);
    private readonly SemaphoreSlim _albumsLock = new(1, 1);
    private readonly SemaphoreSlim _playlistsLock = new(1, 1);

    public event EventHandler<LibraryChangedEventArgs>? Updated;
    
    public Task InspectLibraryAsync(CancellationToken ct = default)
    {
        return _innerLibrary?.InspectLibraryAsync(ct) ?? Task.CompletedTask;
    }

    public async Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        await _artistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return null;
            return await _innerLibrary.GetArtistAsync(artistId, cancellationToken);
        }
        finally
        {
            _artistsLock.Release();
        }
    }

    public async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default)
    {
        await _artistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return [];
            return await _innerLibrary.GetArtistsAsync(query, cancellationToken);
        }
        finally
        {
            _artistsLock.Release();
        }
    }

    public async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        await _artistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return [];
            return await _innerLibrary.GetArtistsAsync(cancellationToken);
        }
        finally
        {
            _artistsLock.Release();
        }
    }

    public async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        await _albumsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return [];
            return await _innerLibrary.GetAlbumsAsync(cancellationToken);
        }
        finally
        {
            _albumsLock.Release();
        }
    }

    public async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        await _albumsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return [];
            return await _innerLibrary.GetAlbumsAsync(artistId, cancellationToken); 
        }
        finally
        {
            _albumsLock.Release();
        }
    }

    public Task<Stream> GetAlbumResourceStreamAsync(Id assetId, CancellationToken token)
    {
        return _innerLibrary?.GetAlbumResourceStreamAsync(assetId, token) ?? Task.FromResult(Stream.Null);
    }

    public async Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default)
    {
        await _albumsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return null;
            return await _innerLibrary.GetAlbumAsync(albumId, cancellationToken);
        }
        finally
        {
            _albumsLock.Release();
        }
    }

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.SearchAsync(query, cancellationToken) ?? Task.FromResult(new SearchResults());
    }

    public async Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        await _playlistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return [];
            return await _innerLibrary.GetPlaylistsAsync(cancellationToken);
        }
        finally
        {
            _playlistsLock.Release();
        }
    }

    public async Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        await _playlistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary == null) return null;
            return await _innerLibrary.GetPlaylistAsync(playlistId, cancellationToken);
        }
        finally
        {
            _playlistsLock.Release();
        }
    }

    public Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetItemAsync(id, cancellationToken) ?? Task.FromResult<Info?>(null);
    }

    public async Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default)
    {
        await _playlistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary != null)
                await _innerLibrary.DeletePlaylistAsync(id, cancellationToken);
        }
        finally
        {
            _playlistsLock.Release();
        }
    }

    public async Task RenamePlaylistAsync(Id id, string newName, CancellationToken cancellationToken = default)
    {
        await _playlistsLock.WaitAsync(cancellationToken);
        try
        {
            if (_innerLibrary != null)
                await _innerLibrary.RenamePlaylistAsync(id, newName, cancellationToken);
        }
        finally
        {
            _playlistsLock.Release();
        }
    }

    public Task BeginRefreshAsync()
    {
        return _innerLibrary?.BeginRefreshAsync() ?? Task.CompletedTask;
    }

    internal void Attach(ILibrarySource library)
    {
        _innerLibrary = library;
        _innerLibrary.Updated += InnerLibraryOnUpdated;
    }
    
    internal void Detach()
    {
        _innerLibrary?.Updated -= InnerLibraryOnUpdated;
        _innerLibrary = null;
    }

    private void InnerLibraryOnUpdated(object? sender, LibraryChangedEventArgs e)
    {
        Updated?.Invoke(sender, e);
    }
}