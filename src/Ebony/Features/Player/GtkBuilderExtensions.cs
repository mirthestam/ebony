using Ebony.Features.Player.Queue;
using Ebony.Hosting;

namespace Ebony.Features.Player;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithPlayerGTypes()
        {
            // Queue
            builder.WithGType<Queue.Queue>();
            builder.WithGType<SaveQueueDialog>();
            
            // Common
            builder.WithGType<MediaControls>();
            builder.WithGType<PlaybackControls>();
            builder.WithGType<Player>();
            builder.WithGType<TrackListItem>();
        }
    }
}