using GObject;
using Gtk;

namespace Ebony.Features.Shell;

#pragma warning disable CS0649
[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(ConnectingPage)}.ui")]
public partial class ConnectingPage
{

}