using System.Text;
using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

/// <summary>
/// MPD Albums are identified by their title + album artist IDs to avoid collisions between
/// different albums with the same title (e.g., "Greatest Hits").
/// </summary>
public class AlbumId : Id.TypedId<string>
{
    public const string Key = "ALB";
    
    public string Title { get; }
    public IReadOnlyList<ArtistId> AlbumArtistIds { get; }

    // Safe separators (identity uses Base64Url so these won't occur inside parts)
    private const char PartSeparator = '\u001F'; // Unit Separator
    private const char ListSeparator = '\u001E'; // Record Separator

    private AlbumId(string value, string title, IReadOnlyList<ArtistId> albumArtistIds)
        : base(value, Key)
    {
        Title = title;
        AlbumArtistIds = albumArtistIds;
    }

    /// <summary>
    /// Creates a new AlbumId based upon information found in a context
    /// </summary>
    public static AlbumId FromContext(AlbumBaseIdentificationContext context)
    {
        var title = (context.Album.Title).Trim();
        
        var artistIds = context.Album.CreditsInfo.AlbumArtists
            .Select(a => a.Artist.Id)
            .OfType<ArtistId>()
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        title = title.Trim();

        var artists = artistIds
            .OrderBy(a => a.ToString(), StringComparer.Ordinal)
            .ToArray();

        var value = Serialize(title, artists);
        return new AlbumId(value, title, artists);
    }
    

    /// <summary>
    /// Used for deserialization, including drag-and-drop scenarios.
    /// </summary>
    public static AlbumId Parse(string value, Func<string, ArtistId> parseArtistId)
{
        if (string.IsNullOrWhiteSpace(value))
            return new AlbumId(string.Empty, string.Empty, Array.Empty<ArtistId>());

        var parts = value.Split(PartSeparator);
        var titlePart = parts[0];
        var title = DecodePart(titlePart);        
        var artists = Array.Empty<ArtistId>();
        if (parts.Length < 2 || string.IsNullOrEmpty(parts[1])) return new AlbumId(value, title, artists);
        
        var encodedArtistIdStrings = parts[1].Split(ListSeparator);
        artists = encodedArtistIdStrings
            .Select(DecodePart)                 // decode back to artist-id-string
            .Select(parseArtistId)          // convert string -> ArtistId object
            .OrderBy(a => a.ToString(), StringComparer.Ordinal)
            .ToArray();

        return new AlbumId(value, title, artists);
    }

    private static string Serialize(string title, IReadOnlyList<ArtistId> artists)
    {
        var encodedTitle = EncodePart(title);

        if (artists.Count == 0)
            return encodedTitle;
        
        var encodedArtists = string.Join(
            ListSeparator,
            artists.Select(a => EncodePart(a.Value)));

        return $"{encodedTitle}{PartSeparator}{encodedArtists}";
    }

    // Base64Url encoding
    private static string EncodePart(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string DecodePart(string value)
    {
        value ??= string.Empty;
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        var bytes = Convert.FromBase64String(s);
        return Encoding.UTF8.GetString(bytes);
    }
}