using Ebony.Backends.MPD.Connection;
using Ebony.Core.Connection;
using Ebony.Features.Shell.Welcome;
using Gio;
using Gtk;

namespace Ebony.Backends.MPD.UI.Connect;

public sealed class ConnectDialogPresenter : IConnectDialogPresenter
{
    public bool CanHandle(IConnectionProfile profile) => profile is ConnectionProfile;

    public Task<ConnectDialogResult> ShowAsync(Widget parent, IConnectionProfile profile,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(profile);

        if (profile is not ConnectionProfile mpdProfile)
            throw new NotSupportedException(
                $"MPD connect dialog cannot handle profile type '{profile.GetType().FullName}'.");

        var workingCopy = new ConnectionProfile
        {
            Name = mpdProfile.Name,
            AutoConnect = mpdProfile.AutoConnect,
            Flags = mpdProfile.Flags,
            Socket = mpdProfile.Socket,
            UseSocket = mpdProfile.UseSocket,
            Host = mpdProfile.Host,
            Port = mpdProfile.Port,
            Password = mpdProfile.Password
        };

        var tcs = new TaskCompletionSource<ConnectDialogResult?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var dialog = ConnectDialog.NewWithProperties([]);
        dialog.ConnectionName = workingCopy.Name;
        dialog.Host = workingCopy.Host;
        dialog.Port = workingCopy.Port;
        dialog.Password = workingCopy.Password;
        dialog.AutoConnect = workingCopy.AutoConnect;
        dialog.Forgettable = workingCopy.Flags.HasFlag(ConnectionFlags.Saved);
    
        CancellationTokenRegistration ctr = default;

        dialog.CancelAction.OnActivate += OnCancel;
        dialog.ConnectAction.OnActivate += OnConnect;
        dialog.ForgetAction.OnActivate += OnForget;

        if (ct.CanBeCanceled)
        {
            ctr = ct.Register(() => Complete(null));
        }

        dialog.Present(parent);
        return tcs.Task;

        void Complete(ConnectDialogResult result)
        {
            if (!tcs.TrySetResult(result)) return;

            Cleanup();
            dialog.Close();
        }

        void OnConnect(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
        {
            if (!dialog.ValidateAndMarkErrors(out var error))
            {
                dialog.ShowToast(error);
                return;
            }

            workingCopy.Name = dialog.ConnectionName ?? string.Empty;
            workingCopy.Host = dialog.Host ?? string.Empty;
            workingCopy.Port = dialog.Port;
            workingCopy.Password = dialog.Password;
            workingCopy.AutoConnect = dialog.AutoConnect;

            mpdProfile.Name = workingCopy.Name;
            mpdProfile.AutoConnect = workingCopy.AutoConnect;
            mpdProfile.Flags = workingCopy.Flags;
            mpdProfile.Socket = workingCopy.Socket;
            mpdProfile.UseSocket = workingCopy.UseSocket;
            mpdProfile.Host = workingCopy.Host;
            mpdProfile.Port = workingCopy.Port;
            mpdProfile.Password = workingCopy.Password;

            Complete(new ConnectDialogResult(ConnectDialogOutcome.Confirmed, mpdProfile));
        }

        void OnCancel(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
        {
            Complete(new ConnectDialogResult(ConnectDialogOutcome.Cancelled, null));
        }

        void OnForget(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
        {
            Complete(new ConnectDialogResult(ConnectDialogOutcome.Forgotten, null));
        }

        void Cleanup()
        {
            dialog.CancelAction.OnActivate -= OnCancel;
            dialog.ConnectAction.OnActivate -= OnConnect;
            dialog.ForgetAction.OnActivate -= OnForget;

            ctr.Dispose();
        }
    }
}