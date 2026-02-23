using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Playlists;

[Subclass<Adw.Dialog>]
[Template<AssemblyResource>($"ui/{nameof(RenamePlaylistDialog)}.ui")]
public partial class RenamePlaylistDialog
{
    [Connect("entry")] private Entry _entry;
    [Connect("rename-button")] private Button _renameButton;
    [Connect("cancel-button")] private Button _cancelButton;
    
    private IPlaylistNameValidator? _nameValidator;
    
    private TaskCompletionSource<bool>? _tcs;    
    
    public string PlaylistName => _entry.GetText();
    
    partial void Initialize()
    {
        _entry.OnChanged += EntryOnOnChanged;
        
        _renameButton.OnClicked += RenameButtonOnOnClicked;
        _cancelButton.OnClicked += CancelButtonOnOnClicked;
    }

    private void CancelButtonOnOnClicked(Button sender, EventArgs args)
    {
        _tcs?.SetResult(false);      // user canceled
        Close();
    }

    private void RenameButtonOnOnClicked(Button sender, EventArgs args)
    {
        if (!Validate()) return;
        _tcs?.SetResult(true);   // user confirmed
        Close();
    }
    
    private void EntryOnOnChanged(Editable sender, EventArgs args) => Validate();

    private bool Validate()
    {
        var text = _entry.GetText();
        var validated = _nameValidator?.Validate(text) ?? false;

        if (validated)
        {
            _entry.RemoveCssClass(AdwStyles.Colors.Error);
        }
        else
        {
            _entry.AddCssClass(AdwStyles.Colors.Error);            
        }
        
        return validated;
    }
    
    public Task<bool> ShowForPlaylistAsync(PlaylistModel playlist, IPlaylistNameValidator nameValidator, Widget parent)
    {
        _nameValidator = nameValidator;
        _entry.SetText(playlist.Name);
        _entry.GrabFocus();

        _tcs = new TaskCompletionSource<bool>();
        Present(parent);

        return _tcs.Task;
    }    
}