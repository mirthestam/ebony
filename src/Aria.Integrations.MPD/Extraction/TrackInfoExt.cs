using Aria.Core.Library;

namespace Aria.Backends.MPD.Extraction;

public static class TrackInfoExt
{
    extension(TrackInfo track)
    {
        public TrackInfo WithMPDAsset()
        {
            if (track.FileName == null) return track;

            return track with
            {
                Assets =
                [
                    new AssetInfo
                    {
                        Id = new AssetId(track.FileName),
                        Type = AssetType.FrontCover
                    }
                ]
            };
        }
    }
}