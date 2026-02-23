using GObject;
using Gtk;

namespace Aria.Features.Browser.Playlists;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(PlaylistsEmptyPage)}.ui")]
public partial class PlaylistsEmptyPage;