using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class IdProvider : IIdProvider
{
    public Id CreateQueueTrackId(QueueTrackBaseIdentificationContext context) => QueueTrackId.FromContext(context);
    
    public Id CreateTrackId(TrackBaseIdentificationContext context) => TrackId.FromContext(context);

    public Id CreateArtistId(ArtistBaseIdentificationContext context) => ArtistId.FromContext(context);

    public Id CreateAlbumId(AlbumBaseIdentificationContext context) => AlbumId.FromContext(context);


public Id Parse(string id)
{
    // Remove surrounding single quotes if present (handles escaped/quoted values)
    // This should either remove ' or "
    if (id is (['\'', _, ..] and [.., '\'']) or (['"', _, ..] and [.., '"']))
    {
        id = id[1..^1];
    }    
    
    var parts = id.Split(Id.KeySeparator);
    if (parts.Length < 2) throw new ArgumentException("Invalid ID format");
    
    var value = parts[1];

    // Route to the appropriate ID parser based on the key prefix
        return parts[0] switch
        {
            PlaylistId.Key => PlaylistId.Parse(value),
            ArtistId.Key => ArtistId.Parse(value),
            TrackId.Key => TrackId.Parse(value),
            AlbumId.Key => AlbumId.Parse(value, ArtistId.Parse),
            QueueTrackId.Key => QueueTrackId.Parse(value),
            AssetId.Key => AssetId.Parse(value),
            _ => throw new NotSupportedException($"Unknown ID key: `{parts[0]}`")
        };
    }
}