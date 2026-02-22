using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.AlbumPage.ui")]
public partial class AlbumPage
{
    private AlbumInfo _album;

    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("tracks-box")] private Box _tracksBox;
    
    [Connect("credit-box")] private CreditBox _creditBox;
    
    [Connect("title-label")] private Label _titleLabel;

    [Connect("message-listbox")] private ListBox _messageListBox;
    [Connect("filter-message-row")]private ActionRow _filterMessageRow;

    [Connect("enqueue-split-button")] private SplitButton _enqueueSplitButton;
    
    private IReadOnlyList<AlbumTrackInfo> _filteredTracks;
    private readonly List<TrackGroup> _trackGroups = [];
    private List<TrackArtistInfo> _sharedArtists = [];

    partial void Initialize()
    {
        InitializeActions();
    }
    
    public void LoadAlbum(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        if (filteredArtist != null)
        {
            _filteredTracks = album.Tracks
                .Where(t => t.Track.CreditsInfo.Artists.Any(a => a.Artist.Id == filteredArtist.Id)).ToList();
            if (_filteredTracks.Count != album.Tracks.Count)
            {
                _filterMessageRow.Title =$"Tracks featuring {filteredArtist.Name}";
                _messageListBox.Visible = true; 
            }
        }
        else
        {
            _filteredTracks = album.Tracks;
            _messageListBox.Visible = false;
        }

        _album = album;

        SetTitle(album.Title);
        
        // Always update the header first. It needs updated shared artists from the header.
        UpdateHeader();
        UpdateTracks();
    }

    private void UpdateTracks()
    {
        foreach (var group in _trackGroups)
        {
            group.RemoveTracks();
            _tracksBox.Remove(group);
        }

        _trackGroups.Clear();

        if (_filteredTracks.Count == 0)
            return;

        List<AlbumTrackInfo> currentGroupTracks = [];

        string? currentGroupKey = null;
        string? currentGroupHeader = null;

        foreach (var track in _filteredTracks)
        {
            var trackGroupKey = track.Group?.Key;
            var trackGroupHeader = track.Group?.Header;

            // This changes grouping to the Work.
            // The problem here is, that the works are multi level.
            // And this would result in a group per _part_ of a movement.
            // There is a fix for this in picard which has recursive groups.
            // But then to enable this, we should have a setting.
            
            // if (track.Track.Work?.ShowMovement == true)
            // {
            //     // This track needs to be grouped by its work and movement,
            //     // Instead of its defined group.
            //     trackGroupKey = track.Track.Work.MovementNumber;
            //     trackGroupHeader = track.Track.Work.MovementName;
            // }
            
            // group boundary?
            if (currentGroupKey != trackGroupKey && currentGroupTracks.Count > 0)
            {
                _ = CreateTrackGroup(currentGroupHeader, _sharedArtists);
            }
            
            if (currentGroupKey != trackGroupKey)
            {
                currentGroupKey = trackGroupKey;
                currentGroupHeader = trackGroupHeader;
            }

            currentGroupTracks.Add(track);
        }
        
        if (currentGroupTracks.Count > 0)
        {
            _ = CreateTrackGroup(currentGroupHeader, _sharedArtists);
        }

        if (_trackGroups.Count != 1) return;

        // Disable the header if there is only one group
        var mainGroup = _trackGroups[0];
        if (string.IsNullOrWhiteSpace(mainGroup.Header))
        {
            _trackGroups[0].HeaderVisible = false;
        }

        return;

        List<AlbumTrackInfo> CreateTrackGroup(string? headerText, IReadOnlyList<TrackArtistInfo> sharedArtists)
        {
            var trackGroup = TrackGroup.NewWithProperties([]);
            trackGroup.LoadTracks(currentGroupTracks, headerText, sharedArtists);
            _tracksBox.Append(trackGroup);
            currentGroupTracks = [];
            _trackGroups.Add(trackGroup);
            return currentGroupTracks;
        }
    }

    public override void Dispose()
    {
        _coverPicture.SetPaintable(null);

        foreach (var group in _trackGroups)
        {
            group.RemoveTracks();
            _tracksBox.Remove(group);
            group.Dispose();
        }
        _trackGroups.Clear();
        
        base.Dispose();
    }

    public void SetCoverArt(Art art)
    {
        _coverPicture.SetPaintable(art.Paintable);
    }

    private void UpdateHeader()
    {
        // Artists that appear on any track are displayed in the header.
        // Otherwise, they are only shown within their respective groups.
        // AlbumsArtists without any tracks are also included in this header.
        
        var sharedArtists = CreditsTools.GetCommonArtistsAcrossTracks(_album.Tracks).ToList();
        var sharedArtistIds = sharedArtists.Select(a => a.Artist.Id).ToHashSet();

        var allTrackArtistIds = _album.Tracks
            .SelectMany(t => t.Track.CreditsInfo.Artists.Select(a => a.Artist.Id))
            .ToHashSet();

        var filteredAlbumArtists = _album.CreditsInfo.AlbumArtists
            .Where(a => sharedArtistIds.Contains(a.Artist.Id) || !allTrackArtistIds.Contains(a.Artist.Id));

        _sharedArtists = sharedArtists.Union(filteredAlbumArtists.Select(a =>
        {
            // Move album artists over to track artists. If a role of an album is not present,
            // Just make it unknown to appear in the credits box.
            var roles = a.Roles;
            if (roles == ArtistRoles.None)
            {
                roles = ArtistRoles.Unknown;
            }
            return new TrackArtistInfo
            {
                Artist = a.Artist,
                Roles = roles,
                IsFeatured = true
            };
        })).ToList();
        
        _creditBox.UpdateTracksCredits(_sharedArtists);
        
        _titleLabel.SetLabel(_album.Title);
    }
}