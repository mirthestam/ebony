using Adw;
using Gio;
using GLib;
using GObject;
using Gtk;
using Box = Gtk.Box;
using Button = Gtk.Button;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Aria.Features.Shell.Welcome;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(WelcomePage)}.ui")]
public partial class WelcomePage
{
    private readonly List<PreferencesRow> _visibleRows = [];
    private readonly List<PreferencesRow> _savedRows = [];

    [Connect("new-button")]  private Button _newButton;
    [Connect("visible-group")] private ListBox _visiblePreferencesGroup;
    [Connect("saved-group")] private ListBox _savedPreferencesGroup;
    
    [Connect("discover-spinner")]private Adw.Spinner _discoverSpinner;
    
    private SimpleActionGroup _welcomeActionGroup;
    
    public SimpleAction NewAction { get; private set; }    
    public SimpleAction ConnectAction { get; private set; }
    public SimpleAction ConfigureAction { get; private set; }
    public SimpleAction ForgetAction { get; private set; }
    
    partial void Initialize()
    {
        InsertActions();
    }

    public bool Discovering
    {
        set => _discoverSpinner.Visible = value;
    }

    public void RefreshConnections(IEnumerable<ConnectionModel> connections)
    {
        foreach (var oldRow in _visibleRows)
        {
            _visiblePreferencesGroup.Remove(oldRow);
        }
        _visibleRows.Clear();

        foreach (var oldRow in _savedRows)
        {
            _savedPreferencesGroup.Remove(oldRow);
        }
        _savedRows.Clear();
        
        foreach (var connection in connections)
        {
            var row = ActionRow.New();
            row.SetTitle(connection.DisplayName);
            row.SetSubtitle(connection.Details);
            row.SetActivatable(true); // They are not activatable by default
            
            row.SetActionName("welcome.connect");
            row.SetActionTargetValue(Variant.NewString(connection.Id.ToString()));
            
            // Workaround because for some reason the actions don't bubble down the tree
            row.InsertActionGroup("welcome", _welcomeActionGroup);
            
            var settingsButton = Button.NewFromIconName("view-more-symbolic");
            settingsButton.AddCssClass("flat");
            settingsButton.SetActionName("welcome.configure");
            settingsButton.SetActionTargetValue(Variant.NewString(connection.Id.ToString()));
            row.AddSuffix(settingsButton);                
            
            if (connection.IsDiscovered)
            {
                _visiblePreferencesGroup.Append(row);
                _visibleRows.Add(row);                
            }
            else
            {
                _savedPreferencesGroup.Append(row);
                _savedRows.Add(row);                    
            }
        }
        
        _savedPreferencesGroup.Visible = _savedRows.Count != 0;
        _visiblePreferencesGroup.Visible = _visibleRows.Count != 0;        
    }

    private void InsertActions()
    {
        _welcomeActionGroup = SimpleActionGroup.New();
        _welcomeActionGroup.AddAction(NewAction = SimpleAction.New("new", null));
        _welcomeActionGroup.AddAction(ConnectAction = SimpleAction.New("connect", VariantType.New("s")));
        _welcomeActionGroup.AddAction(ConfigureAction = SimpleAction.New("configure", VariantType.New("s")));
        _welcomeActionGroup.AddAction(ForgetAction = SimpleAction.New("forget", VariantType.New("s")));
        InsertActionGroup("welcome", _welcomeActionGroup);
        
        // Workaround because for some reason the actions don't bubble down the tree
        _newButton.InsertActionGroup("welcome", _welcomeActionGroup);
        _visiblePreferencesGroup.InsertActionGroup("welcome", _welcomeActionGroup);
        _savedPreferencesGroup.InsertActionGroup("welcome", _welcomeActionGroup);
    }    
}