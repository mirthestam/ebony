using Ebony.Core.Library;

namespace Ebony.Infrastructure;

public static class RolesFormatting
{
    public static string Format(ArtistRoles artistRoles)
    {
        var roles = new List<string>();
        if (artistRoles.HasFlag(ArtistRoles.Composer)) roles.Add("Composer");
        if (artistRoles.HasFlag(ArtistRoles.Arranger)) roles.Add("Arranger");
        if (artistRoles.HasFlag(ArtistRoles.Conductor)) roles.Add("Conductor");
        if (artistRoles.HasFlag(ArtistRoles.Ensemble)) roles.Add("Ensemble");
        if (artistRoles.HasFlag(ArtistRoles.Performer)) roles.Add("Performer");
        if (artistRoles.HasFlag(ArtistRoles.Soloist)) roles.Add("Soloist");
        return string.Join(", ", roles);       
    }
}