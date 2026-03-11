using Ebony.Core;
using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Gio;
using GLib;

namespace Ebony.Features.Browser.Album;

public partial class AlbumPage
{
    // Actions
    private SimpleAction _albumEnqueueDefaultAction;
    private SimpleAction _albumEnqueueReplaceAction;
    private SimpleAction _albumEnqueueNextAction;
    private SimpleAction _albumEnqueueEndAction;
    
    public SimpleAction ShowFullAlbumAction { get; private set; }
    public SimpleAction EnqueueTrack { get; private set; }
    
    private void InitializeActions()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_albumEnqueueDefaultAction = SimpleAction.New("enqueue-default", null));        
        actionGroup.AddAction(_albumEnqueueReplaceAction = SimpleAction.New("enqueue-replace", null));
        actionGroup.AddAction(_albumEnqueueNextAction = SimpleAction.New("enqueue-next", null));
        actionGroup.AddAction(_albumEnqueueEndAction = SimpleAction.New("enqueue-end", null));
        actionGroup.AddAction(ShowFullAlbumAction = SimpleAction.New("full", null));
        actionGroup.AddAction(EnqueueTrack = SimpleAction.New("enqueue-track-default", VariantType.String));
        InsertActionGroup("album", actionGroup);
        //_enqueueSplitButton.InsertActionGroup("album", actionGroup);
        
        _albumEnqueueDefaultAction.OnActivate += AlbumEnqueueDefaultActionOnOnActivate;
        _albumEnqueueReplaceAction.OnActivate += AlbumEnqueueReplaceActionOnOnActivate;
        _albumEnqueueEndAction.OnActivate += AlbumEnqueueEndActionOnOnActivate;
        _albumEnqueueNextAction.OnActivate += AlbumEnqueueNextActionOnOnActivate;
    }    
    
    private void AlbumEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueFromAction(EnqueueAction.EnqueueNext);
    private void AlbumEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueFromAction(EnqueueAction.EnqueueEnd);
    private void AlbumEnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueFromAction(EnqueueAction.Replace);
    private void AlbumEnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueFromAction(null);

    private void EnqueueFromAction(EnqueueAction? enqueueAction = IQueue.DefaultEnqueueAction)
    {
        var trackList = _filteredTracks.Select(t => t.Track.Id.ToString()).ToArray();

        switch (enqueueAction)
        {
            case EnqueueAction.Replace:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", Variant.NewStrv(trackList));
                break;
            
            case EnqueueAction.EnqueueEnd:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", Variant.NewStrv(trackList));
                break;                
            
            case EnqueueAction.EnqueueNext:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", Variant.NewStrv(trackList));
                break;
            
            case null:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", Variant.NewStrv(trackList));
                break;
                
            default:
                throw new ArgumentOutOfRangeException(nameof(enqueueAction), enqueueAction, null);
        }
    }
}