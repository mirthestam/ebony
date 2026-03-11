using System.Collections.Concurrent;
using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Tags;

namespace Aria.Backends.MPD;

public record ArtistAliasesCacheEntry(IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases);

public partial class Library
{
    // Artists can appear with multiple notations in our backend.
    // This means we 'deduplicated' them using our tag parser.
    // However, for a lookup, we need to use those aliases to make sure
    // we are leaving nothing out.
    private readonly ConcurrentDictionary<Id, ConcurrentDictionary<string, byte>> _artistAliases = new();

    // Artists
    public override async Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        // It is not a problem that we are using All Artists here.
        // GetArtistsAsync is cached, so this is efficient.
        var artists = await GetArtistsAsync(cancellationToken).ConfigureAwait(false);
        return artists.FirstOrDefault(artist => artist.Id == artistId);
    }

    public override async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query,
        CancellationToken cancellationToken = default)
    {
        var artists = await GetArtistsAsync(cancellationToken).ConfigureAwait(false);

        if (query.RequiredRoles is { } requiredRoles)
            artists = artists.Where(a => (a.Roles & requiredRoles) != 0); // OR operator effectively

        artists = query.Sort switch
        {
            ArtistSort.ByName => artists.OrderBy(a => a.Name),
            _ => artists
        };

        return artists;
    }

    public override async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        return await LoadArtistsWithRecoveryAsync(cancellationToken);
    }

    private async Task<List<ArtistInfo>> LoadArtistsWithRecoveryAsync(CancellationToken cancellationToken)
    {
        const string allArtistsIndexKey = "artists:all:index";

        var index = await cache.GetOrAddAsync(allArtistsIndexKey,
            async () => await CacheArtistsFromBackend(cancellationToken),
            cancellationToken);
        if (index == null) return [];

        if (_artistAliases.IsEmpty)
        {
            await LoadAliasesFromCache(cancellationToken);
        }

        var loadArtistTasks = index.ItemKeys
            .Select(key =>
                cache.GetOrAddAsync<ArtistInfo?>(key, () => Task.FromResult<ArtistInfo?>(null), cancellationToken))
            .ToList();

        var artists = await Task.WhenAll(loadArtistTasks);
        var artistsList = artists.Where(a => a != null).Cast<ArtistInfo>().ToList();

        if (artistsList.Count == index.ItemKeys.Count) return artistsList;
        
        logger.LogWarning("The retrieved number of artists does not equal the number of artists in the index. Rebuilding cache.");
        cache.Invalidate(allArtistsIndexKey);
        return await LoadArtistsWithRecoveryAsync(cancellationToken);
    }

    private async Task LoadAliasesFromCache(CancellationToken cancellationToken)
    {
        var aliasesEntry = await cache.GetOrAddAsync("artists:aliases",
            () => Task.FromResult<ArtistAliasesCacheEntry?>(null), cancellationToken);
        if (aliasesEntry?.Aliases == null) return;

        foreach (var (artistName, aliases) in aliasesEntry.Aliases)
        {
            var artistId = new ArtistId(artistName);
            var dict = _artistAliases.GetOrAdd(artistId, _ => new ConcurrentDictionary<string, byte>());
            foreach (var alias in aliases)
            {
                dict[alias] = 0;
            }
        }
    }

    private async Task<CollectionCacheEntry?> CacheArtistsFromBackend(CancellationToken cancellationToken)
    {
        var artistMap = new Dictionary<Id, ArtistInfo>();

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        await FetchAndAddSingles(new ListCommand(MpdTags.AlbumArtist), ArtistRoles.None, scope, true)
            .ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Artist), ArtistRoles.Performer, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Composer), ArtistRoles.Composer, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Performer), ArtistRoles.Performer, scope)
            .ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(ExtraMpdTags.Conductor), ArtistRoles.Conductor, scope)
            .ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(ExtraMpdTags.Ensemble), ArtistRoles.Ensemble, scope)
            .ConfigureAwait(false);

        var artists = artistMap.Values.ToList();

        var cacheArtistTasks = artists.Select(async a =>
        {
             await cache.GetOrAddAsync<ArtistInfo?>($"artist:{a.Id}", () => Task.FromResult<ArtistInfo?>(a), cancellationToken);
            return $"artist:{a.Id}";
        }).ToList();

        // Cache all artists
        var keys = await Task.WhenAll(cacheArtistTasks);

        // Also cache aliases
        var aliasesDict = _artistAliases.ToDictionary(
            kvp => ((ArtistId)kvp.Key).Value, IReadOnlyList<string> (kvp) => kvp.Value.Keys.ToList()
        );
        await cache.GetOrAddAsync("artists:aliases",
            () => Task.FromResult<ArtistAliasesCacheEntry?>(new ArtistAliasesCacheEntry(aliasesDict)),
            cancellationToken);

        return new CollectionCacheEntry(keys, DateTimeOffset.UtcNow);

        async Task FetchAndAddSingles(IMpcCommand<IEnumerable<string>> command, ArtistRoles role,
            ConnectionScope connectionScope, bool featured = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (isSuccess, result) = await connectionScope.SendCommandAsync(command).ConfigureAwait(false);
            if (isSuccess && result != null)
            {
                foreach (var name in result)
                    AddOrUpdate(backendArtistName: name, artistNameSort: null, roles: role, featured);
            }
        }

        void AddOrUpdate(string backendArtistName, string? artistNameSort, ArtistRoles roles, bool featured)
        {
            if (string.IsNullOrWhiteSpace(backendArtistName)) return;

            var info = tagParser.ParseArtist(backendArtistName, artistNameSort, roles);
            if (info == null) return;
            if (string.IsNullOrWhiteSpace(info.Name)) return;

            var id = new ArtistId(info.Name);

            if (artistMap.TryGetValue(id, out var existingArtist))
            {
                artistMap[id] = existingArtist with
                {
                    NameSort = info.NameSort ?? existingArtist.NameSort,
                    Roles = existingArtist.Roles | info.Roles,
                    IsFeatured = existingArtist.IsFeatured || featured
                };
            }
            else
            {
                artistMap[id] = info with
                {
                    Id = id,
                    IsFeatured = featured
                };
            }

            _artistAliases.GetOrAdd(id, _ => new ConcurrentDictionary<string, byte>())[backendArtistName] = 0;
        }
    }
}