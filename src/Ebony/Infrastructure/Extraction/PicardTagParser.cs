using System.Globalization;
using Ebony.Core.Extraction;

namespace Ebony.Infrastructure.Extraction;

public partial class PicardTagParser(IIdProvider idProvider) : ITagParser
{
    /// <summary>
    /// Contains all the tags that we can process using the Picard tag parser
    /// </summary>
    /// <summary>
    ///     A tag parser that follows tags as defined by the default configuration of MusicBrainz Picard
    ///     https://mpd.readthedocs.io/en/latest/protocol.html#tags
    /// </summary>
    private static PicardTagInfo ParseTags(IReadOnlyList<Tag> tags)
    {
        var parsed = new PicardTagInfo();

        foreach (var tag in tags)
        {
            // TODO: Queue information is actually MPD specific and should be moved.
            if (tag.Name.Equals(PicardTags.QueueTags.Position, StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(tag.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pos))
                    parsed.QueuePosition = pos;
                continue;
            }

            if (tag.Name.Equals(PicardTags.FileTags.File, StringComparison.OrdinalIgnoreCase))
            {
                parsed.FileName = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.TrackTags.Date, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Date = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.TrackTags.Duration, StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(tag.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    parsed.Duration = TimeSpan.FromSeconds(seconds);
                continue;
            }

            if (tag.Name.Equals(PicardTags.TrackTags.Title, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Title = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.AlbumTags.Album, StringComparison.OrdinalIgnoreCase))
            {
                parsed.AlbumTitle = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.WorkTags.Work, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Work = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.WorkTags.Movement, StringComparison.OrdinalIgnoreCase))
            {
                parsed.MovementName = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.WorkTags.MovementNumber, StringComparison.OrdinalIgnoreCase))
            {
                parsed.MovementNumber = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.WorkTags.ShowMovement, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ShowMovement = tag.Value == "1";
                continue;
            }

            if (tag.Name.Equals(PicardTags.ArtistTags.Artist, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ArtistTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTags.AlbumTags.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                parsed.AlbumArtistTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTags.ArtistTags.Composer, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ComposerTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTags.ArtistTags.Performer, StringComparison.OrdinalIgnoreCase))
            {
                parsed.PerformerTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTags.ArtistTags.Ensemble, StringComparison.OrdinalIgnoreCase))
            {
                parsed.EnsembleTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTags.ArtistTags.Conductor, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Conductor = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.AlbumTags.Disc, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Disc = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.AlbumTags.Track, StringComparison.OrdinalIgnoreCase))
            {
                parsed.TrackNumber = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTags.GroupTags.Heading, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Heading = tag.Value;
            }
        }

        return parsed;
    }

    private sealed class PicardTagInfo
    {
        public uint? QueuePosition { get; set; }

        public string FileName { get; set; } = "";
        public string Date { get; set; } = "";
        public string Title { get; set; } = "";
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

        public string AlbumTitle { get; set; } = "";

        public string Work { get; set; } = "";
        public string MovementName { get; set; } = "";
        public string MovementNumber { get; set; } = "";
        public bool ShowMovement { get; set; }

        public string Disc { get; set; } = "";
        public string TrackNumber { get; set; } = "";
        public string Heading { get; set; } = "";

        public List<string> ArtistTags { get; } = [];
        public List<string> AlbumArtistTags { get; } = [];
        public List<string> ComposerTags { get; } = [];
        public List<string> PerformerTags { get; } = [];
        public List<string> EnsembleTags { get; } = [];
        public string Conductor { get; set; } = "";
    }
}