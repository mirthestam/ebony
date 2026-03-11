using Ebony.Core.Library;

namespace Ebony.Core.Extraction;

/// <summary>
/// A parser for extracting information from track metadata (tags and their values)
/// </summary>
public interface ITagParser
{
    /// <summary>
    /// Parses all queue-track-related information from the tags
    /// </summary>
    QueueTrackInfo ParseQueueTrack(IReadOnlyList<Tag> sourceTags);
    
    /// <summary>
    /// Parses all album-track-related information from the tags
    /// </summary>
    /// <remarks>
    /// </remarks>
    AlbumTrackInfo ParseAlbumTrack(IReadOnlyList<Tag> sourceTags);
    
    /// <summary>
    ///     Parses all album-related information from the tags
    /// </summary>
    AlbumInfo ParseAlbum(IReadOnlyList<Tag> sourceTags);
    
    /// <summary>
    /// Is used by the library to determine how to format artists
    /// </summary>
    ArtistInfo? ParseArtist(string artistName, string? artistNameSort, ArtistRoles roles);
}