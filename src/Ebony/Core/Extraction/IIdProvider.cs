namespace Ebony.Core.Extraction;

public interface IIdProvider
{
    Id CreateQueueTrackId(QueueTrackBaseIdentificationContext context);
    Id CreateTrackId(TrackBaseIdentificationContext context);
    Id CreateArtistId(ArtistBaseIdentificationContext context);
    Id CreateAlbumId(AlbumBaseIdentificationContext context);

    /// <summary>
    ///     Parses a string representation of an ID back into its strongly typed ID object.
    /// </summary>
    /// <param name="id">The string ID in the format "KEY::value" where KEY identifies the ID type.</param>
    /// <remarks>
    ///     The method splits the ID string by "::" to extract the key and value parts.
    ///     If the value is wrapped in single quotes (e.g., 'value'), they are trimmed off.
    ///     The key determines which specific ID type parser is invoked.
    /// </remarks>    
    Id Parse(string id);    
}