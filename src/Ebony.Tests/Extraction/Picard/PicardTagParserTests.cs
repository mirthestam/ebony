using Ebony.Backends.MPD.Extraction;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure.Extraction;

namespace Ebony.Tests.Extraction.Picard;

public class PicardTagParserTests
{
    private readonly PicardTagParser _parser;

    public PicardTagParserTests()
    {
        // The MPD ID factory is used here to identify unique items.
        // Note that the MPD tag parser is also compatible with other backends.
        var idFactory = new IdProvider();
        _parser = new PicardTagParser(idFactory);
    }

    [Fact]
    public void ParseAlbum_ParsesTitle()
    {
        // Arrange
        List<Tag> tags =
        [
            new(PicardTags.AlbumTags.Album, "My amazing album"),
            new(PicardTags.AlbumTags.AlbumArtist, "Mirthe Stam"),
            new(PicardTags.TrackTags.Title, "My amazing song")
        ];

        // Act
        var info = _parser.ParseAlbum(tags);

        // Assert
        Assert.Equal("My amazing album", info.Title);
    }


    [Fact]
    public void ParseAlbum_AlbumArtist_GetsRoleFromTrack()
    {
        // This test checks if an album artist gets the role they have on a track.

        // Arrange
        List<Tag> tags =
        [
            // Album
            new(PicardTags.AlbumTags.Album, "My amazing album"),
            new(PicardTags.AlbumTags.AlbumArtist, "Lang Lang"),
            
            // Track
            new(PicardTags.TrackTags.Title, "My amazing song"),
            new(PicardTags.ArtistTags.Performer, "Lang Lang")
        ];

        // Act
        var info = _parser.ParseAlbum(tags);

        // Assert
        var albumArtist = info.CreditsInfo.AlbumArtists[0];
        Assert.True(albumArtist.Roles.HasFlag(ArtistRoles.Performer));
    }
    
    [Fact]
    public void ParseAlbum_Artist_HasFeaturedFlag()
    {
        // This test if the album artist has its 'isfeatured' flag always set.
        
        // Arrange
        List<Tag> tags =
        [
            // Album
            new(PicardTags.AlbumTags.Album, "My amazing album"),
            new(PicardTags.AlbumTags.AlbumArtist, "Lang Lang"),
            
            // Track
            new(PicardTags.TrackTags.Title, "My amazing song"),
            new(PicardTags.ArtistTags.Performer, "Lang Lang")
        ];

        // Act
        var info = _parser.ParseAlbum(tags);

        // Assert
        var artist = info.CreditsInfo.Artists[0];
        Assert.True(artist.IsFeatured);
    }

    [Fact]
    public void ParseAlbumTrack_AlbumArtistNotOnTrack_IsNotExposedAsTrackArtist()
    {
        // Make sure, that an album artist not present on a track, is not exposed as an artist for this track
        // Arrange
        List<Tag> tags =
        [
            // Album
            new(PicardTags.AlbumTags.Album, "My amazing album"),
            new(PicardTags.AlbumTags.AlbumArtist, "Lang Lang"),
            
            // Track
            new(PicardTags.TrackTags.Title, "My amazing song"),
            new(PicardTags.ArtistTags.Performer, "Someone Else")
        ];
        
        
        // Act
        var info = _parser.ParseAlbumTrack(tags);
        var artist = info.Track.CreditsInfo.Artists[0];

        // Assert
        
        Assert.False(artist.IsFeatured);
        Assert.NotEqual("Lang Lang", artist.Artist.Name);
    }
    
}