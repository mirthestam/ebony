namespace Ebony.Infrastructure.Extraction;


/// <summary>
/// Constants for names of tags as used by the Picard tagger
/// https://picard-docs.musicbrainz.org/en/index.html
/// </summary>
public static class PicardTags
{
    public static class AlbumTags
    {
        public const string Album = "album";
        public const string AlbumArtist = "albumartist";
        public const string Track = "track";
        public const string Disc = "disc";
    }

    public static class FileTags
    {
        public const string File = "file";
    }

    public static class TrackTags
    {
        public const string Duration = "duration";
        public const string Title = "title";
        public const string Name = "name";
        public const string Genre = "genre";
        public const string Comment = "comment";
        public const string Date = "date";
    }

    public static class ArtistTags
    {
        public const string Artist = "artist";
        public const string Composer = "composer";
        public const string ComposerSort = "composersort";
        public const string Conductor = "conductor";
        public const string Performer = "performer";
        public const string Ensemble = "ensemble";
    }

    public static class WorkTags
    {
        public const string Work = "work";
        public const string Movement = "movement";
        public const string MovementNumber = "movementnumber";
        public const string ShowMovement = "showmovement";
    }

    public static class GroupTags
    {
        public const string Heading = "grouping";
    }

    public static class RecordingTags
    {
        public const string Location = "location";
    }

    public static class QueueTags
    {
        public const string Position = "pos";
    }
}