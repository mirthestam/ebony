using Adw;
using Ebony.Infrastructure;
using Gio;
using GObject;
using Gtk;
using Dialog = Adw.Dialog;

namespace Ebony.Backends.MPD.UI.Connect;

[Subclass<Dialog>]
[Template<AssemblyResource>("Ebony.Backends.MPD.UI.Connect.ConnectDialog.ui")]
public partial class ConnectDialog
{
    [Connect("cancel-button")] private Button _cancelButton;
    [Connect("connect-button")] private Button _connectButton;

    [Connect("name-row")] private EntryRow _nameRow;
    [Connect("host-row")] private EntryRow _hostRow;
    [Connect("port-row")] private SpinRow _portRow;
    [Connect("password-row")] private PasswordEntryRow _passwordRow;

    [Connect("autoconnect-row")] private SwitchRow _autoConnectRow;

    [Connect("forget-button")]private Button _forgetButton;

    [Connect("toast-overlay")] private ToastOverlay _toastOverlay;

    public SimpleAction CancelAction { get; private set; }
    public SimpleAction ConnectAction { get; private set; }
    public SimpleAction ForgetAction { get; private set; }
    
    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(CancelAction = SimpleAction.New("cancel", null));
        actionGroup.AddAction(ConnectAction = SimpleAction.New("connect", null));
        actionGroup.AddAction(ForgetAction = SimpleAction.New("forget", null));
        InsertActionGroup("connect", actionGroup);

        _nameRow.OnChanged += OnAnyInputChanged;
        _hostRow.OnChanged += OnAnyInputChanged;

        // SpinRow is not Editable; update enabled state when value changes.
        _portRow.OnNotify += (_, _) => UpdateConnectEnabled();

        UpdateConnectEnabled();
    }

    public string? ConnectionName
    {
        get => _nameRow.Text_;
        set => _nameRow.Text_ = value ?? string.Empty;
    }

    public bool AutoConnect
    {
        get => _autoConnectRow.Active;
        set => _autoConnectRow.Active = value;
    }

    public string? Host
    {
        get => _hostRow.Text_;
        set => _hostRow.Text_ = value ?? string.Empty;
    }

    public int Port
    {
        get => (int)_portRow.Value;
        set => _portRow.Value = value;
    }

    public string? Password
    {
        get => _passwordRow.Text_;
        set => _passwordRow.Text_ = value ?? string.Empty;
    }

    public bool Forgettable
    {
        set => _forgetButton.Visible = value;
    }

    public void ShowToast(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        _toastOverlay.AddToast(Toast.New(message));
    }

    public bool ValidateAndMarkErrors(out string error)
    {
        var nameOk = !string.IsNullOrWhiteSpace(_nameRow.Text_);
        SetErrorState(_nameRow, !nameOk);

        var hostText = _hostRow.Text_ ?? string.Empty;
        var hostOk = !string.IsNullOrWhiteSpace(hostText) && IsValidHostname(hostText);
        SetErrorState(_hostRow, !hostOk);

        var port = (int)_portRow.Value;
        var portOk = port is >= 1 and <= 65535;
        SetErrorState(_portRow, !portOk);

        if (!nameOk)
        {
            error = "Name is required.";
            return false;
        }

        if (!hostOk)
        {
            error = "Host is invalid.";
            return false;
        }

        if (!portOk)
        {
            error = "Port must be between 1 and 65535.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void OnAnyInputChanged(Editable sender, EventArgs args)
    {
        // Live feedback, but do not spam toasts here.
        ValidateAndMarkErrors(out _);
        UpdateConnectEnabled();
    }

    private void UpdateConnectEnabled()
    {
        var valid = ValidateAndMarkErrors(out _);
        ConnectAction.Enabled = valid;
    }

    private static void SetErrorState(Widget widget, bool isError)
    {
        if (isError) widget.AddCssClass(AdwStyles.Colors.Error);
        else widget.RemoveCssClass(AdwStyles.Colors.Error);
    }

    private static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return false;

        if (System.Net.IPAddress.TryParse(hostname, out _))
            return true;

        if (hostname.Length > 253)
            return false;

        var labels = hostname.Split('.');
        foreach (var label in labels)
        {
            if (string.IsNullOrEmpty(label) || label.Length > 63)
                return false;

            if (label.StartsWith('-') || label.EndsWith('-'))
                return false;

            if (!label.All(c => char.IsLetterOrDigit(c) || c == '-'))
                return false;
        }

        return true;
    }
}