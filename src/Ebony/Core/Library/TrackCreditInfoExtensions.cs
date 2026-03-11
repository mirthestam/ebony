namespace Ebony.Core.Library;

/// <summary>
/// Convenience methods to filter for common 'roles' on the artists
/// </summary>
public static class TrackCreditInfoExtensions
{
    extension(TrackCreditsInfo info)
    {
        public IEnumerable<TrackArtistInfo> Composers => info.Artists.Where(x => x.Roles.HasFlag(ArtistRoles.Composer));
        
        public IEnumerable<TrackArtistInfo> Conductors => info.Artists.Where(x => x.Roles.HasFlag(ArtistRoles.Conductor));
        
        public IEnumerable<TrackArtistInfo> OtherArtists => info.Artists.Where(x =>
            !x.Roles.HasFlag(ArtistRoles.Composer) &&
            !x.Roles.HasFlag(ArtistRoles.Conductor));        
    }
}