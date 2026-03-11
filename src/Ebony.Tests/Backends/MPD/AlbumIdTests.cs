using Ebony.Backends.MPD.Extraction;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using GLib;
using DateTime = System.DateTime;

namespace Ebony.Tests;

public class AlbumIdTests
{
    [Fact]
    void TestAlbumIdParsing()
    {
        var expectedArtistId = ArtistId.Parse("Chopin");
        var context = new AlbumBaseIdentificationContext
        {
            Album = new AlbumInfo
            {
                Title = "Great album",
                ReleaseDate = DateTime.Now,
                CreditsInfo = new AlbumCreditsInfo
                {
                    AlbumArtists = new List<AlbumArtistInfo>
                    {
                        new()
                        {
                            Roles = ArtistRoles.None,
                            Artist = new()
                                {
                                Id = expectedArtistId,
                                Name = "Chopin"
                            }
                        }
                    }
                },
                Id = Id.Undetermined
            }
        };
        var albumId = AlbumId.FromContext(context);

        var serialized = albumId.ToString();
        
        // Get the value part
        var parts = serialized.Split(Id.KeySeparator);
        var parsed = AlbumId.Parse(parts[1], ArtistId.Parse);
        
        Assert.Equal(AlbumId.Key, parts[0]);
        Assert.Equal(expectedArtistId, parsed.AlbumArtistIds[0]);
    }
}