using Aria.Core.Library;
using Aria.Infrastructure;
using GLib;
using GObject;
using Gtk;
using TimeSpan = GLib.TimeSpan;

namespace Aria.Features.Browser.Album;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(TrackGroup)}.ui")]
public partial class TrackGroup
{
    [Connect("tracks-listbox")] private ListBox _tracksListBox;
    [Connect("header-label")]  private Label _headerLabel;
    [Connect("duration-label")] private Label _durationlabel;
    [Connect("credit-box")] private CreditBox _creditBox;
    [Connect("header-box")] private Box _headerBox;
    
    [Connect("track-popover-menu")] private PopoverMenu _trackPopoverMenu;    
    
    private IReadOnlyList<TrackArtistInfo> _albumSharedArtists = [];    
    private List<AlbumTrackInfo> _tracks = [];
    
    public bool HeaderVisible
    {
        get => _headerBox.Visible;
        set => _headerBox.Visible = value;
    }

    public string? Header
    {
        get => _headerLabel.Label_;
        private set => _headerLabel.Label_ = value;
    }
    
    public string? Duration
    {
        get => _durationlabel.Label_;
        private set => _durationlabel.Label_ = value;
    }    
    
    partial void Initialize()
    {
        InitializeActions();
    }
    
    public void LoadTracks(List<AlbumTrackInfo> tracks, string? headerText,
        IReadOnlyList<TrackArtistInfo> albumSharedArtists)
    {
        _tracks = tracks;
        _albumSharedArtists = albumSharedArtists;
        
        if (tracks.Count == 1)
        {
            // Just one track. Header does not make sense
            Header = null;
            Duration = null;
        }
        else
        {
            Header = headerText;

            var duration = _tracks.Aggregate(System.TimeSpan.Zero, (current, t) => current.Add(t.Track.Duration));
            
            // TODO: Duration formatting is duplicate. Reuse.
            Duration = duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"mm\:ss");            
        }

        UpdateHeader();
        UpdateTracksList();
    }

    public void RemoveTracks()
    {
        _tracksListBox.RemoveAll();
    }

    private void UpdateTracksList()
    {
        RemoveTracks();

        if (_tracks.Count == 0)
        {
            _tracksListBox.SetVisible(false);
            return;
        }
        
        foreach (var albumTrack in _tracks)
        {
            // TODO: We're constructing list items in code here.
            // It would be better to define this via a .UI template.

            // If an album is by "AlbumArtist A", we don't want to repeat "Artist A" next to every track.
            // We only want to show guest artists or different collaborators.

            var track = albumTrack.Track;

            var trackNumberText = albumTrack switch
            {
                { TrackNumber: { } t, VolumeName: { } d and not "" } => $"{d}.{t}",
                { TrackNumber: { } t } => t.ToString(),
                _ => null
            };

            var row = AlbumTrackRow.NewForTrackId(track.Id);
            
            var prefixLabel = Label.New(trackNumberText);
            prefixLabel.AddCssClass("numeric");
            prefixLabel.AddCssClass(AdwStyles.Dimmed);
            prefixLabel.SetXalign(1);
            prefixLabel.WidthChars = 4;
            row.AddPrefix(prefixLabel);

            var suffixLabel = Label.New(track.Duration.ToString(@"mm\:ss"));
            suffixLabel.AddCssClass("numeric");
            suffixLabel.AddCssClass(AdwStyles.Dimmed);

            row.AddSuffix(suffixLabel);
            row.SetUseMarkup(false);

            row.SetTitle(track.Work?.ShowMovement == true
                ? track.Work.MovementName
                : track.Title);
            
            var rightClickGesture = GestureClick.NewWithProperties([]);
            rightClickGesture.Button = 3;
            rightClickGesture.OnPressed += TrackGestureClickOnOnPressed;
            row.AddController(rightClickGesture);

            var longPressGesture = GestureLongPress.NewWithProperties([]);
            longPressGesture.OnPressed += LongPressGestureOnOnPressed;
            longPressGesture.TouchOnly = true;
            row.AddController(longPressGesture);            
            
            var guestArtists = CreditsTools.GetTrackSpecificArtists(track, _tracks);
            var subTitleLine = string.Join(", ", guestArtists.Select(a => a.Artist.Name));

            row.SetSubtitle(subTitleLine);

            row.SetActivatable(true);
            row.SetActionName("album.enqueue-track-default");
            row.SetActionTargetValue(Variant.NewString(track.Id.ToString()));

            InitializeTrackDragSource(row);

            _tracksListBox.Append(row);
        }
    }

    private void UpdateHeader()
    {
        var sharedArtists = CreditsTools.GetCommonArtistsAcrossTracks(_tracks).ToList();
        
        // Remove the shared artists from the album's shared artists list.
        // This way we don't duplicate information from the album header.
        sharedArtists = sharedArtists.Except(_albumSharedArtists).ToList();
        
        _creditBox.UpdateTracksCredits(sharedArtists);
    }
}