using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Core.Queue;
using Ebony.Features.Player.Queue;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Ebony.Features.Player;

public partial class PlayerPresenter
{
    // Actions
    private SimpleAction _ebonyPlayerNextTrackAction;
    private SimpleAction _ebonyPlayerPreviousTrackAction;
    private SimpleAction _ebonyPlayerPlayPauseAction;
    private SimpleAction _ebonyPlayerStopAction;
    
    private SimpleAction _ebonyQueueEnqueueDefaultAction;
    private SimpleAction _ebonyQueueEnqueueReplaceAction;
    private SimpleAction _ebonyQueueEnqueueNextAction;
    private SimpleAction _ebonyQueueEnqueueEndAction;
    private SimpleAction _ebonyQueueClearAction;
    private SimpleAction _ebonyQueueRemoveTrackAction;

    private SimpleAction _ebonyQueueShuffleAction;
    private SimpleAction _ebonyQueueRepeatAction;
    private SimpleAction _ebonyQueueConsumeAction;
    
    private SimpleAction _ebonyQueueSaveAction;

    private void InitializeActions(AttachContext context)
    {
        var playerActionGroup = SimpleActionGroup.New();
        playerActionGroup.AddAction(_ebonyPlayerNextTrackAction = SimpleAction.New(AppActions.Player.Next.Action, null));
        playerActionGroup.AddAction(_ebonyPlayerPreviousTrackAction = SimpleAction.New(AppActions.Player.Previous.Action, null));
        playerActionGroup.AddAction(_ebonyPlayerPlayPauseAction = SimpleAction.New(AppActions.Player.PlayPause.Action, null));
        playerActionGroup.AddAction(_ebonyPlayerStopAction = SimpleAction.New(AppActions.Player.Stop.Action, null));
        context.InsertAppActionGroup(AppActions.Player.Key, playerActionGroup);
        
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Next.Action}", [AppActions.Player.Next.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Previous.Action}", [AppActions.Player.Previous.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.PlayPause.Action}", [AppActions.Player.PlayPause.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Stop.Action}", [AppActions.Player.Stop.Accelerator]);       
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_ebonyQueueClearAction = SimpleAction.New(AppActions.Queue.Clear.Action, null));
        queueActionGroup.AddAction(_ebonyQueueSaveAction = SimpleAction.New(AppActions.Queue.Save.Action, null));
        queueActionGroup.AddAction(_ebonyQueueEnqueueDefaultAction = SimpleAction.New(AppActions.Queue.EnqueueDefault.Action, VariantType.NewArray(VariantType.String)));        
        queueActionGroup.AddAction(_ebonyQueueEnqueueReplaceAction = SimpleAction.New(AppActions.Queue.EnqueueReplace.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ebonyQueueEnqueueNextAction = SimpleAction.New(AppActions.Queue.EnqueueNext.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ebonyQueueEnqueueEndAction = SimpleAction.New(AppActions.Queue.EnqueueEnd.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_ebonyQueueRemoveTrackAction = SimpleAction.New(AppActions.Queue.RemoveTrack.Action, VariantType.String));
        queueActionGroup.AddAction(_ebonyQueueShuffleAction = SimpleAction.NewStateful(AppActions.Queue.Shuffle.Action, null, Variant.NewBoolean(false)));
        _ebonyQueueRepeatAction = SimpleAction.NewStateful(AppActions.Queue.Repeat.Action, VariantType.String, Variant.NewString(nameof(RepeatMode.Disabled)));
        queueActionGroup.AddAction(_ebonyQueueRepeatAction);
        queueActionGroup.AddAction(_ebonyQueueConsumeAction = SimpleAction.NewStateful(AppActions.Queue.Consume.Action, null, Variant.NewBoolean(false)));
        context.InsertAppActionGroup(AppActions.Queue.Key, queueActionGroup);
        
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Save.Action}", [AppActions.Queue.Save.Accelerator]);        
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}", [AppActions.Queue.Clear.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Shuffle.Action}", [AppActions.Queue.Shuffle.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Consume.Action}", [AppActions.Queue.Consume.Accelerator]);
        
        _ebonyPlayerNextTrackAction.OnActivate += EbonyPlayerNextTrackActionOnOnActivate;
        _ebonyPlayerPreviousTrackAction.OnActivate += EbonyPlayerPreviousTrackActionOnOnActivate;        
        _ebonyPlayerPlayPauseAction.OnActivate += EbonyPlayerPlayPauseActionOnOnActivate;
        _ebonyPlayerStopAction.OnActivate += EbonyPlayerStopActionOnOnActivate;
        
        _ebonyQueueClearAction.OnActivate += EbonyQueueClearActionOnOnActivate;
        _ebonyQueueSaveAction.OnActivate += EbonyQueueSaveActionOnOnActivate;
        
        _ebonyQueueEnqueueDefaultAction.OnActivate += DefaultEbonyQueueEnqueueActionOnOnActivate;
        _ebonyQueueEnqueueEndAction.OnActivate += EbonyQueueEnqueueEndActionOnOnActivate;
        _ebonyQueueEnqueueNextAction.OnActivate += EbonyQueueEnqueueNextActionOnOnActivate;
        _ebonyQueueEnqueueReplaceAction.OnActivate += PlayActionOnOnActivate;        
        _ebonyQueueRemoveTrackAction.OnActivate += EbonyQueueRemoveTrackActionOnOnActivate;
        
        _ebonyQueueShuffleAction.OnActivate += EbonyQueueShuffleActionOnOnActivate;
        _ebonyQueueRepeatAction.OnChangeState += EbonyQueueRepeatActionOnOnChangeState;
        _ebonyQueueConsumeAction.OnActivate += EbonyQueueConsumeActionOnOnActivate;
    }
    
    private async void EbonyQueueRepeatActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        try
        {
            var value = args.Value.GetString(out _);
            var mode = Enum.Parse<RepeatMode>(value);
            await _ebony.Queue.SetRepeatAsync(mode);
        }
        catch (Exception e)
        {
            LogFailedToSetRepeat(e);
            _messenger.Send(new ShowToastMessage("Failed to set repeat"));
        
            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewString(_ebony.Queue.Repeat.Mode.ToString()));
        }
    }

    private async void EbonyQueueConsumeActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ebony.Queue.SetConsumeAsync(!_ebony.Queue.Consume.Enabled);
        }
        catch (Exception e)
        {
            LogFailedToSetConsume(e);
            _messenger.Send(new ShowToastMessage("Failed to set consume"));

            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewBoolean(_ebony.Queue.Consume.Enabled));
        }
    }
    
    private async void EbonyQueueShuffleActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ebony.Queue.SetShuffleAsync(!_ebony.Queue.Shuffle.Enabled);
        }
        catch (Exception e)
        {
            LogFailedToSetShuffle(e);
            _messenger.Send(new ShowToastMessage("Failed to set shuffle"));

            // Revert UI state to actual current value if call fails
            sender.SetState(Variant.NewBoolean(_ebony.Queue.Shuffle.Enabled));
        }
    }

    private async void EbonyQueueRemoveTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedId = args.Parameter!.GetString(out _);
            var id = _ebonyControl.Parse(serializedId);
        
            // Need to remove these queueTrackId's from the queue.
            await _ebony.Queue.RemoveTrackAsync(id);
        }
        catch(Exception e)
        {
            LogFailedToRemoveTracks(e);
            _messenger.Send(new ShowToastMessage($"Failed to remove track(s)."));
        }
    }

    private void DefaultEbonyQueueEnqueueActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(IQueue.DefaultEnqueueAction, args);
    private void EbonyQueueEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueEnd, args);
    private void EbonyQueueEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueNext, args);
    private void PlayActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.Replace, args);

    private async void EnqueueHandler(EnqueueAction enqueueAction, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedIds = args.Parameter!.GetStrv(out _);
            var ids = serializedIds.Select(_ebonyControl.Parse).ToArray();
            
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
            var item =await _ebony.Library.GetItemAsync(id).ConfigureAwait(false);
            if (item == null) continue;
            items.Add(item);
        }

        await _ebony.Queue.EnqueueAsync(items, action).ConfigureAwait(false);
        
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
    
    private async void EbonyQueueSaveActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var dialog = SaveQueueDialog.NewWithProperties([]);
            var result = await dialog.ShowAsync(_playlistNameValidator, View!);
            if (!result) return;
            
            await _ebony.Queue.SaveOrAppendToPlaylistAsync(dialog.PlaylistName);
            _messenger.Send(new ShowToastMessage($"Saved queue as '{dialog.PlaylistName}'."));
        }
        catch (Exception ex) 
        {
            LogFailedToSaveQueue(ex);
        }
    }
    
    private async void EbonyQueueClearActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _ebony.Queue.ClearAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void EbonyPlayerStopActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _ebony.Player.StopAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void EbonyPlayerPreviousTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _ebony.Player.PreviousAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to previous track"));
        }
    }

    private async void EbonyPlayerNextTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ebony.Player.NextAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next track"));
        }
    }    

    private async void EbonyPlayerPlayPauseActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            switch (_ebony.Player.State)
            {
                case PlaybackState.Paused:
                    await _ebony.Player.ResumeAsync();
                    break;
                case PlaybackState.Stopped:
                    var currentTrack = _ebony.Queue.CurrentTrack;
                    await _ebony.Player.PlayAsync(currentTrack?.Position ?? 0);
                    break;
                case PlaybackState.Playing:
                    await _ebony.Player.PauseAsync();
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
            var info = await _ebony.Library.GetItemAsync(id);
            if (info == null) return;

            _ = _ebony.Queue.EnqueueAsync(info, IQueue.DefaultEnqueueAction);
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