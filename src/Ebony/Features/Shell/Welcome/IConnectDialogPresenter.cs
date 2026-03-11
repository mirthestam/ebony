using Ebony.Core.Connection;

namespace Ebony.Features.Shell.Welcome;

public interface IConnectDialogPresenter
{
    bool CanHandle(IConnectionProfile profile);
    Task<ConnectDialogResult> ShowAsync(Gtk.Widget parent, IConnectionProfile profile, CancellationToken ct = default);
}