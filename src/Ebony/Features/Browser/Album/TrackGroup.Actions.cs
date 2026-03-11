using Ebony.Core;
using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Gdk;
using Gio;
using GLib;
using Gtk;

namespace Ebony.Features.Browser.Album;

public partial class TrackGroup
{
    private SimpleAction _groupEnqueueDefaultAction;
    private SimpleAction _groupEnqueueReplaceAction;
    private SimpleAction _groupEnqueueNextAction;
    private SimpleAction _groupEnqueueEndAction;

    private SimpleAction _trackShowInformationAction;
    private SimpleAction _trackEnqueueDefaultAction;
    private SimpleAction _trackEnqueueReplaceAction;
    private SimpleAction _trackEnqueueNextAction;
    private SimpleAction _trackEnqueueEndAction;

    private AlbumTrackRow _contextMenuRow;

    private void InitializeActions()
    {
        // The group is the header actions
        // We also need track level actions

        // _trackGestureClick.OnPressed += TrackGestureClickOnOnPressed;

        const string group = "group";
        const string groupEnqueueDefault = "enqueue-default";
        const string groupEnqueueReplace = "enqueue-replace";
        const string groupEnqueueNext = "enqueue-next";
        const string groupEnqueueEnd = "enqueue-end";

        var groupActionGroup = SimpleActionGroup.New();
        groupActionGroup.AddAction(_groupEnqueueDefaultAction = SimpleAction.New(groupEnqueueDefault, null));
        groupActionGroup.AddAction(_groupEnqueueReplaceAction = SimpleAction.New(groupEnqueueReplace, null));
        groupActionGroup.AddAction(_groupEnqueueNextAction = SimpleAction.New(groupEnqueueNext, null));
        groupActionGroup.AddAction(_groupEnqueueEndAction = SimpleAction.New(groupEnqueueEnd, null));
        InsertActionGroup(group, groupActionGroup);

        _groupEnqueueDefaultAction.OnActivate += GroupEnqueueDefaultActionOnOnActivate;
        _groupEnqueueReplaceAction.OnActivate += GroupEnqueueReplaceActionOnOnActivate;
        _groupEnqueueNextAction.OnActivate += GroupEnqueueNextActionOnOnActivate;
        _groupEnqueueEndAction.OnActivate += GroupEnqueueEndActionOnOnActivate;

        const string track = "track";
        const string trackShowInformation = "show-information";
        const string trackEnqueueDefault = "enqueue-default";
        const string trackEnqueueReplace = "enqueue-replace";
        const string trackEnqueueNext = "enqueue-next";
        const string trackEnqueueEnd = "enqueue-end";

        var trackActionGroup = SimpleActionGroup.New();
        trackActionGroup.AddAction(_trackShowInformationAction = SimpleAction.New(trackShowInformation, null));
        trackActionGroup.AddAction(_trackEnqueueDefaultAction = SimpleAction.New(trackEnqueueDefault, null));
        trackActionGroup.AddAction(_trackEnqueueReplaceAction = SimpleAction.New(trackEnqueueReplace, null));
        trackActionGroup.AddAction(_trackEnqueueNextAction = SimpleAction.New(trackEnqueueNext, null));
        trackActionGroup.AddAction(_trackEnqueueEndAction = SimpleAction.New(trackEnqueueEnd, null));
        InsertActionGroup(track, trackActionGroup);

        _trackEnqueueDefaultAction.OnActivate += TrackEnqueueDefaultActionOnOnActivate;
        _trackEnqueueNextAction.OnActivate += TrackEnqueueNextActionOnOnActivate;
        _trackEnqueueEndAction.OnActivate += TrackEnqueueEndActionOnOnActivate;
        _trackEnqueueReplaceAction.OnActivate += TrackEnqueueReplaceActionOnOnActivate;
        _trackShowInformationAction.OnActivate += TrackShowInformationActionOnOnActivate;

        var trackRowMenu = Menu.New();
        trackRowMenu.AppendItem(MenuItem.New("Show Details", $"{track}.{trackShowInformation}"));

        var trackRowEnqueueMenu = Menu.New();

        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{track}.{trackEnqueueReplace}");
        trackRowEnqueueMenu.AppendItem(replaceQueueItem);

        var playNextItem = MenuItem.New("Play after current track", $"{track}.{trackEnqueueNext}");
        trackRowEnqueueMenu.AppendItem(playNextItem);

        var playLastItem = MenuItem.New("Add to queue", $"{track}.{trackEnqueueEnd}");
        trackRowEnqueueMenu.AppendItem(playLastItem);

        trackRowMenu.AppendSection(null, trackRowEnqueueMenu);

        _trackPopoverMenu.SetMenuModel(trackRowMenu);


        var groupController = ShortcutController.New();

        // Group
        groupController.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"),
            NamedAction.New($"{group}.{groupEnqueueDefault}")));

        AddController(groupController);

        var trackController = ShortcutController.New();

        // Track
        trackController.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"),
            NamedAction.New($"{track}.{trackEnqueueEnd}"))); // TODO: this should be based upon the default
        trackController.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Alt>Return"),
            NamedAction.New($"{track}.{trackShowInformation}")));
        
        AddController(trackController);
    }
    
    private void TrackShowInformationActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var parameter = Variant.NewString(_contextMenuRow.TrackId.ToString());
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowTrack.Action}", parameter);
    }

    private void TrackEnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var parameter = Variant.NewString(_contextMenuRow.TrackId.ToString());
        var parameterArray = Variant.NewArray(VariantType.String, [parameter]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", parameterArray);
    }

    private void TrackEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var parameter = Variant.NewString(_contextMenuRow.TrackId.ToString());
        var parameterArray = Variant.NewArray(VariantType.String, [parameter]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", parameterArray);
    }

    private void TrackEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var parameter = Variant.NewString(_contextMenuRow.TrackId.ToString());
        var parameterArray = Variant.NewArray(VariantType.String, [parameter]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", parameterArray);
    }

    private void TrackEnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var parameter = Variant.NewString(_contextMenuRow.TrackId.ToString());
        var parameterArray = Variant.NewArray(VariantType.String, [parameter]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", parameterArray);
    }

    private void LongPressGestureOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        var row = (AlbumTrackRow)sender.Widget!;
        ShowContextMenu(row, args.X, args.Y);
    }

    private void ShowContextMenu(AlbumTrackRow row, double x, double y)
    {
        _contextMenuRow = row;
        
        // Calculate the popover menu position by transforming coordinates:
        // Since the popover menu is attached to the parent and shared across rows,
        // we need to combine all three coordinate spaces to get the absolute position.
        _contextMenuRow.GetBounds(out var rowX, out var rowY, out _, out _);
        _tracksListBox.GetBounds(out var boxX, out var boxY, out _, out _);

        var rect = new Rectangle
        {
            X = (int)Math.Round(boxX + rowX + x),
            Y = (int)Math.Round(boxY + rowY + y),
        };

        _trackPopoverMenu.SetPointingTo(rect);

        if (!_trackPopoverMenu.Visible)
            _trackPopoverMenu.Popup();        
    }
    
    private void TrackGestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        var row = (AlbumTrackRow)sender.Widget!;
        ShowContextMenu(row, args.X, args.Y);
    }

    private void GroupEnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) =>
        Enqueue();

    private void GroupEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) =>
        Enqueue(EnqueueAction.EnqueueEnd);

    private void GroupEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) =>
        Enqueue(EnqueueAction.EnqueueNext);

    private void GroupEnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) =>
        Enqueue(EnqueueAction.Replace);

    private void Enqueue(EnqueueAction? enqueueAction = IQueue.DefaultEnqueueAction)
    {
        var trackList = _tracks.Select(t => t.Track.Id.ToString()).ToArray();

        switch (enqueueAction)
        {
            case EnqueueAction.Replace:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}",
                    Variant.NewStrv(trackList));
                break;
            case EnqueueAction.EnqueueNext:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}",
                    Variant.NewStrv(trackList));
                break;
            case EnqueueAction.EnqueueEnd:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}",
                    Variant.NewStrv(trackList));
                break;
            case null:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}",
                    Variant.NewStrv(trackList));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(enqueueAction), enqueueAction, null);
        }
    }
}