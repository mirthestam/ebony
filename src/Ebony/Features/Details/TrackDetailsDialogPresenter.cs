using Ebony.Core.Library;
using Ebony.Features.Shell;

namespace Ebony.Features.Details;

public class TrackDetailsDialogPresenter : IPresenter<TrackDetailsDialog>
{
    public void Attach(TrackDetailsDialog view)
    {
        View = view;
    }

    public void Load(AlbumTrackInfo trackInfo, AlbumInfo albumInfo)
    {
        View!.LoadTrack(trackInfo, albumInfo);
    }

    public TrackDetailsDialog? View { get; private set; }
}