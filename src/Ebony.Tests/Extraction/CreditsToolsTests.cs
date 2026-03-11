using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

namespace Ebony.Tests.Extraction;

public class CreditsToolsTests
{
    private class StringId(string value) : Id.TypedId<string>(value, "ID");
    
    [Fact]
    public void GetSharedAndUniqueArtists_WithVariousArtistRoles_ReturnsCorrectArtistCategories()
    {
        // Arrange

        var beethoven = new ArtistInfo { Id = new StringId("beethoven"), Name = "Beethoven", Roles = ArtistRoles.Composer };
        var karajan = new ArtistInfo { Id = new StringId("karajan"), Name = "Karajan", Roles = ArtistRoles.Conductor };
        var berliner = new ArtistInfo { Id = new StringId("berliner"), Name = "Berliner", Roles = ArtistRoles.Ensemble };
        var operasinger = new ArtistInfo { Id = new StringId("itzak"), Name = "Itzak", Roles = ArtistRoles.Performer };

        var album = new AlbumInfo
        {
            Id = new StringId("album"),
            Title = "Beethovens 9th",
            CreditsInfo = new AlbumCreditsInfo
            {
                AlbumArtists = new List<AlbumArtistInfo>
                {
                    new() { Artist = beethoven, Roles = ArtistRoles.Composer },
                    new() { Artist = karajan, Roles = ArtistRoles.Ensemble }
                },
                Artists = new List<TrackArtistInfo>
                {
                    new() { Artist = beethoven, Roles = ArtistRoles.Composer },
                    new() { Artist = karajan, Roles = ArtistRoles.Conductor },
                    new() { Artist = berliner, Roles = ArtistRoles.Ensemble },
                    new() { Artist = operasinger, Roles = ArtistRoles.Performer }
                }
            },
            Tracks = new List<AlbumTrackInfo>
            {
                new()
                {
                    Track = new TrackInfo
                    {
                        Id = new StringId("track1"),
                        Duration = TimeSpan.FromSeconds(10),
                        Title = "First movement",
                        CreditsInfo = new TrackCreditsInfo
                        {
                            Artists = new List<TrackArtistInfo>
                            {
                                new()
                                {
                                    Artist = beethoven,
                                    Roles = ArtistRoles.Composer
                                },
                                new()
                                {
                                    Artist = karajan,
                                    Roles = ArtistRoles.Conductor
                                },
                                new()
                                {
                                    Artist = berliner,
                                    Roles = ArtistRoles.Ensemble
                                }
                            },
                            AlbumArtists = new List<AlbumArtistInfo>
                            {
                                new() { Artist = beethoven, Roles = ArtistRoles.Composer },
                                new() { Artist = karajan, Roles = ArtistRoles.Ensemble }
                            },
                        },
                        Work = null,
                        ReleaseDate = null,
                        FileName = null,
                        AlbumId = new StringId("album"),
                    },
                    TrackNumber = 1,
                    VolumeName = null,
                    Id = Id.Undetermined
                },
                new()
                {
                    Track = new TrackInfo
                    {
                        Id = new StringId("track1"),
                        Duration = TimeSpan.FromSeconds(10),
                        Title = "Last movement (Ode to Joy)",
                        CreditsInfo = new TrackCreditsInfo
                        {
                            Artists = new List<TrackArtistInfo>
                            {
                                new()
                                {
                                    Artist = beethoven,
                                    Roles = ArtistRoles.Composer
                                },
                                new()
                                {
                                    Artist = karajan,
                                    Roles = ArtistRoles.Conductor
                                },
                                new()
                                {
                                    Artist = berliner,
                                    Roles = ArtistRoles.Ensemble
                                },
                                new()
                                {
                                    Artist = operasinger,
                                    Roles = ArtistRoles.Performer
                                }
                            },
                            AlbumArtists = new List<AlbumArtistInfo>
                            {
                                new()
                                {
                                    Artist = beethoven,
                                    Roles = ArtistRoles.Composer
                                },
                                new()
                                {
                                    Artist = karajan,
                                    Roles = ArtistRoles.Ensemble
                                }
                            },
                        },
                        Work = null,
                        ReleaseDate = null,
                        FileName = null,
                        AlbumId = new StringId("album"),
                    },
                    TrackNumber = 2,
                    VolumeName = null,
                    Id = Id.Undetermined
                }
            },
            ReleaseDate = DateTime.Now
        };

        // Act
        
        var sharedArtists = CreditsTools.GetCommonArtistsAcrossTracks(album.Tracks).Select(ta => ta.Artist).ToList();
        var uniqueArtists = CreditsTools.GetTrackSpecificArtists(album.Tracks[^1].Track, album.Tracks).Select(ta => ta.Artist).ToList();

        // Assert

        // Beethoven was featured on every track
        Assert.Contains(beethoven, sharedArtists);
        Assert.DoesNotContain(beethoven, uniqueArtists);

        // Karajan was featured on every track
        Assert.Contains(karajan, sharedArtists);
        Assert.DoesNotContain(karajan, uniqueArtists);

        // The berliner was featured on every track
        Assert.Contains(berliner, sharedArtists);
        Assert.DoesNotContain(berliner, uniqueArtists);

        // The opera singer was only featured on the last track, so he should not be a common artist.
        Assert.DoesNotContain(operasinger, sharedArtists);
        Assert.Contains(operasinger, uniqueArtists);
    }
}