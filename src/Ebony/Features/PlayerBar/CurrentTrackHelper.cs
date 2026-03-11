using System.Diagnostics;
using Ebony.Core.Library;

namespace Ebony.Features.PlayerBar;

public static class CurrentTrackHelper
{
    public static string[] GetLines(TrackInfo? trackInfo)
    {
        var titleLine = "";
        var subTitleLine = "";

        if (trackInfo is null)
        {
            return [titleLine, subTitleLine];            
        }
        
        titleLine = trackInfo.Title;
        if (trackInfo.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            titleLine = $"{trackInfo.Work.MovementName} ({trackInfo.Work.MovementNumber} {trackInfo.Title} ({trackInfo.Work.Work})";
        
        var credits = trackInfo.CreditsInfo;

        var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));

        var details = new List<string>();
        var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
        if (!string.IsNullOrEmpty(conductors))
            details.Add($"conducted by {conductors}");

        var composersLine = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
        if (!string.IsNullOrEmpty(composersLine))
            details.Add($"composed by {composersLine}");

        subTitleLine = artists;
        if (details.Count > 0) subTitleLine += $" ({string.Join(", ", details)})";        
        
        return [titleLine, subTitleLine, composersLine];
    }
}