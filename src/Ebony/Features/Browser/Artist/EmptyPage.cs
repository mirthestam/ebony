using GObject;
using Gtk;

namespace Ebony.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(EmptyPage)}.ui")]
public partial class EmptyPage;