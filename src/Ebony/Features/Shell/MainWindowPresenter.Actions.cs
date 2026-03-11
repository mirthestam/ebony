using Adw;
using Ebony.Core;
using Ebony.Infrastructure;
using Gio;

namespace Ebony.Features.Shell;

public partial class MainWindowPresenter
{
    // Actions
    private SimpleAction _aboutAction;
    private SimpleAction _disconnectAction;    
    
    private void InitializeActions(AttachContext context)
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_aboutAction = SimpleAction.New(AppActions.Window.About.Action, null));
        actionGroup.AddAction(_disconnectAction = SimpleAction.New(AppActions.Window.Disconnect.Action, null));
        
        context.InsertAppActionGroup(AppActions.Window.Key, actionGroup);        
        context.SetAccelsForAction($"{AppActions.Window.Key}.{AppActions.Window.About.Action}", [AppActions.Window.About.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Window.Key}.{AppActions.Window.Disconnect.Action}", [AppActions.Window.Disconnect.Accelerator]);
        
        _aboutAction.OnActivate += AboutActionOnOnActivate;        
        _disconnectAction.OnActivate += DisconnectActionOnActivate;
    }    
    
    private async void DisconnectActionOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ebonyControl.StopAsync();
        }
        catch (Exception e)
        {
            ShowToast("Failed to disconnect. Please restart Ebony.");
            LogFailedToDisconnect(e);
        }
        finally
        {
            // Whatever happens; always return to the Welcome page
            View.TogglePage(MainWindow.MainPages.Welcome);
        }
    }

    private void AboutActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var dialog = AboutDialog.NewFromAppdata("/nl/mirthestam/ebony/nl.mirthestam.ebony.metainfo.xml", null);
        dialog.Present(View);
    }    
}