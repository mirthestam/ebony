using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Player;

public partial class PlayerPresenter
{
    // Actions
    private SimpleAction _ariaPlayerNextTrackAction;
    private SimpleAction _ariaPlayerPreviousTrackAction;
    private SimpleAction _ariaPlayerPlayPauseAction;
    private SimpleAction _ariaPlayerStopAction;
    
    private SimpleAction _ariaQueueEnqueueDefaultAction;
    private SimpleAction _ariaQueueEnqueueReplaceAction;
    private SimpleAction _ariaQueueEnqueueNextAction;
    private SimpleAction _ariaQueueEnqueueEndAction;
    private SimpleAction _ariaQueueClearAction;
    private SimpleAction _ariaQueueRemoveTrackAction;

    private SimpleAction _ariaQueueShuffleAction;
    private SimpleAction _ariaQueueRepeatAction;
    private SimpleAction _ariaQueueConsumeAction;
    
    private SimpleAction _ariaQueueSaveAction;

    private void InitializeActions(AttachContext context)
    {
        var playerActionGroup = SimpleActionGroup.New();
        playerActionGroup.AddAction(_ariaPlayerNextTrackAction = SimpleAction.New(AppActions.Player.Next.Action, null));
        playerActionGroup.AddAction(_ariaPlayerPreviousTrackAction = SimpleAction.New(AppActions.Player.Previous.Action, null));
        playerActionGroup.AddAction(_ariaPlayerPlayPauseAction = SimpleAction.New(AppActions.Player.PlayPause.Action, null));
        playerActionGroup.AddAction(_ariaPlayerStopAction = SimpleAction.New(AppActions.Player.Stop.Action, null));
        context.InsertAppActionGroup(AppActions.Player.Key, playerActionGroup);
        
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Next.Action}", [AppActions.Player.Next.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Previous.Action}", [AppActions.Player.Previous.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.PlayPause.Action}", [AppActions.Player.PlayPause.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Stop.Action}", [AppActions.Player.Stop.Accelerator]);       
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_ariaQueueClearAction = SimpleAction.New(AppActions.Queue.Clear.Action, null));
        queueActionGroup.AddAction(_ariaQueueSaveAction = SimpleAction.New(AppActions.Queue.Save.Action, null));
        queueActionGroup.AddAction(_ariaQueueEnqueueDefaultAction = SimpleAction.New(AppActions.Queue.EnqueueDefault.Action, VariantType.NewArray(VariantType.String)));        
        queueActionGroup.AddAction(_ariaQueueEnqueueReplaceAction = SimpleAction.New(AppActions.Queue.EnqueueReplace.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ariaQueueEnqueueNextAction = SimpleAction.New(AppActions.Queue.EnqueueNext.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ariaQueueEnqueueEndAction = SimpleAction.New(AppActions.Queue.EnqueueEnd.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ariaQueueRemoveTrackAction = SimpleAction.New(AppActions.Queue.RemoveTrack.Action, VariantType.String));
        queueActionGroup.AddAction(_ariaQueueShuffleAction = SimpleAction.NewStateful(AppActions.Queue.Shuffle.Action, null, Variant.NewBoolean(false)));
        _ariaQueueRepeatAction = SimpleAction.NewStateful(AppActions.Queue.Repeat.Action, VariantType.String, Variant.NewString(nameof(RepeatMode.Disabled)));
        queueActionGroup.AddAction(_ariaQueueRepeatAction);
        queueActionGroup.AddAction(_ariaQueueConsumeAction = SimpleAction.NewStateful(AppActions.Queue.Consume.Action, null, Variant.NewBoolean(false)));
        context.InsertAppActionGroup(AppActions.Queue.Key, queueActionGroup);
        
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Save.Action}", [AppActions.Queue.Save.Accelerator]);        
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}", [AppActions.Queue.Clear.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Shuffle.Action}", [AppActions.Queue.Shuffle.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Consume.Action}", [AppActions.Queue.Consume.Accelerator]);
        
        _ariaPlayerNextTrackAction.OnActivate += AriaPlayerNextTrackActionOnOnActivate;
        _ariaPlayerPreviousTrackAction.OnActivate += AriaPlayerPreviousTrackActionOnOnActivate;        
        _ariaPlayerPlayPauseAction.OnActivate += AriaPlayerPlayPauseActionOnOnActivate;
        _ariaPlayerStopAction.OnActivate += AriaPlayerStopActionOnOnActivate;
        
        _ariaQueueClearAction.OnActivate += AriaQueueClearActionOnOnActivate;
        _ariaQueueSaveAction.OnActivate += AriaQueueSaveActionOnOnActivate;
        
        _ariaQueueEnqueueDefaultAction.OnActivate += DefaultAriaQueueEnqueueActionOnOnActivate;
        _ariaQueueEnqueueEndAction.OnActivate += AriaQueueEnqueueEndActionOnOnActivate;
        _ariaQueueEnqueueNextAction.OnActivate += AriaQueueEnqueueNextActionOnOnActivate;
        _ariaQueueEnqueueReplaceAction.OnActivate += PlayActionOnOnActivate;        
        _ariaQueueRemoveTrackAction.OnActivate += AriaQueueRemoveTrackActionOnOnActivate;
        
        _ariaQueueShuffleAction.OnActivate += AriaQueueShuffleActionOnOnActivate;
        _ariaQueueRepeatAction.OnChangeState += AriaQueueRepeatActionOnOnChangeState;
        _ariaQueueConsumeAction.OnActivate += AriaQueueConsumeActionOnOnActivate;
    }
    
    private async void AriaQueueRepeatActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        try
        {
            var value = args.Value.GetString(out _);
            var mode = Enum.Parse<RepeatMode>(value);
            await _aria.Queue.SetRepeatAsync(mode);
        }
        catch (Exception e)
        {
            LogFailedToSetRepeat(e);
            _messenger.Send(new ShowToastMessage("Failed to set repeat"));
        
            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewString(_aria.Queue.Repeat.Mode.ToString()));
        }
    }

    private async void AriaQueueConsumeActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _aria.Queue.SetConsumeAsync(!_aria.Queue.Consume.Enabled);
        }
        catch (Exception e)
        {
            LogFailedToSetConsume(e);
            _messenger.Send(new ShowToastMessage("Failed to set consume"));

            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewBoolean(_aria.Queue.Consume.Enabled));
        }
    }
    
    private async void AriaQueueShuffleActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _aria.Queue.SetShuffleAsync(!_aria.Queue.Shuffle.Enabled);
        }
        catch (Exception e)
        {
            LogFailedToSetShuffle(e);
            _messenger.Send(new ShowToastMessage("Failed to set shuffle"));

            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewBoolean(_aria.Queue.Shuffle.Enabled));
        }
    }

    private async void AriaQueueRemoveTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedId = args.Parameter!.GetString(out _);
            var id = _ariaControl.Parse(serializedId);
        
            // Need to remove these queueTrackId's from the queue.
            await _aria.Queue.RemoveTrackAsync(id);
        }
        catch(Exception e)
        {
            LogFailedToRemoveTracks(e);
            _messenger.Send(new ShowToastMessage($"Failed to remove track(s)."));
        }
    }

    private void DefaultAriaQueueEnqueueActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(IQueue.DefaultEnqueueAction, args);
    private void AriaQueueEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueEnd, args);
    private void AriaQueueEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueNext, args);
    private void PlayActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.Replace, args);

    private async void EnqueueHandler(EnqueueAction enqueueAction, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedIds = args.Parameter!.GetStrv(out _);
            var ids = serializedIds.Select(_ariaControl.Parse).ToArray();
            
            // Enqueue the items by id
            await EnqueueIds(enqueueAction, ids).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToEnqueueTracks(e);
            _messenger.Send(new ShowToastMessage($"Failed to enqueue tracks."));
        }        
    }
    
    private async Task EnqueueIds(EnqueueAction action, Id[] ids)
    {
        var items = new List<Info>();
        foreach (var id in ids)
        {
            // Would be great to have 'GetItems' instead of foreach here.
            var item =await _aria.Library.GetItemAsync(id).ConfigureAwait(false);
            if (item == null) continue;
            items.Add(item);
        }

        await _aria.Queue.EnqueueAsync(items, action).ConfigureAwait(false);
        
        switch (action)
        {
            case EnqueueAction.Replace:
                _messenger.Send(new ShowToastMessage($"Playing tracks."));
                break;
            case EnqueueAction.EnqueueNext:
                _messenger.Send(new ShowToastMessage($"Playing tracks Next."));
                break;
            case EnqueueAction.EnqueueEnd:
                _messenger.Send(new ShowToastMessage($"Added tracks to end of queue."));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }    
    
    private async void AriaQueueSaveActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var dialog = SaveQueueDialog.NewWithProperties([]);
            var result = await dialog.ShowAsync(_playlistNameValidator, View!);
            if (!result) return;
            
            await _aria.Queue.SaveOrAppendToPlaylistAsync(dialog.PlaylistName);
            _messenger.Send(new ShowToastMessage($"Saved queue as '{dialog.PlaylistName}'."));
        }
        catch (Exception ex) 
        {
            LogFailedToSaveQueue(ex);
        }
    }
    
    private async void AriaQueueClearActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Queue.ClearAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void AriaPlayerStopActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Player.StopAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void AriaPlayerPreviousTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Player.PreviousAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to previous track"));
        }
    }

    private async void AriaPlayerNextTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _aria.Player.NextAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next track"));
        }
    }    

    private async void AriaPlayerPlayPauseActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            switch (_aria.Player.State)
            {
                case PlaybackState.Paused:
                    await _aria.Player.ResumeAsync();
                    break;
                case PlaybackState.Stopped:
                    var currentTrack = _aria.Queue.CurrentTrack;
                    await _aria.Player.PlayAsync(currentTrack?.Position ?? 0);
                    break;
                case PlaybackState.Playing:
                    await _aria.Player.PauseAsync();
                    break;
            }
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to play/pause track"));
        }
    }
    
    private async void ViewOnEnqueueRequested(object? sender, Id id)
    {
        try
        {
            var info = await _aria.Library.GetItemAsync(id);
            if (info == null) return;

            _ = _aria.Queue.EnqueueAsync(info, IQueue.DefaultEnqueueAction);
        }
        catch (Exception exception)
        {
            _messenger.Send(new ShowToastMessage("Could not enqueue"));
            LogCouldNotEnqueue(exception);
        }
    }    
    
    [LoggerMessage(LogLevel.Error, "Could not enqueue")]
    partial void LogCouldNotEnqueue(Exception e);    
    
    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);    
 
    [LoggerMessage(LogLevel.Error, "Failed to enqueue tracks.")]
    partial void LogFailedToEnqueueTracks(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to save queue")]
    partial void LogFailedToSaveQueue(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set shuffle")]
    partial void LogFailedToSetShuffle(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set repeat")]
    partial void LogFailedToSetRepeat(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set consume")]
    partial void LogFailedToSetConsume(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to remove track(s)")]
    partial void LogFailedToRemoveTracks(Exception ex);
}