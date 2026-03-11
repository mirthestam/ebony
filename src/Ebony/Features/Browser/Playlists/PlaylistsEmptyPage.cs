using GObject;
using Gtk;

namespace Ebony.Features.Browser.Playlists;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(PlaylistsEmptyPage)}.ui")]
public partial class PlaylistsEmptyPage;