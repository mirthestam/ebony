using GObject;
using Gtk;

namespace Ebony.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(BrowserEmptyPage)}.ui")]
public partial class BrowserEmptyPage;