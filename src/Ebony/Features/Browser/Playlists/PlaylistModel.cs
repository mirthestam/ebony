using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using Gdk;
using GObject;
using Object = GObject.Object;

namespace Ebony.Features.Browser.Playlists;

[Subclass<Object>]
public partial class PlaylistModel : INotifyPropertyChanged
{
    public static PlaylistModel NewForPlaylistInfo(PlaylistInfo playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var model = NewWithProperties([]);

        model.PlaylistId = playlist.Id;
        model.Name = playlist.Name;
        model.LastModified = playlist.LastModified;

        if (playlist.Tracks.Count > 0)
        {
            model.CoverArtId = playlist.Tracks[0]
                .Track.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)
                ?.Id;
        }

        var topArtists = playlist.Tracks
            .SelectMany(track => track.Track.CreditsInfo.Artists.Select(artist => artist.Artist.Name))
            .GroupBy(artist => artist)
            .OrderByDescending(group => group.Count())
            .Take(3)
            .Select(group => group.Key);

        model.Credits = string.Join(", ", topArtists);

        return model;
    }

    public DateTime LastModified { get; private set; }

    public Id PlaylistId { get; private set; }

    public Id? CoverArtId { get; private set; }

    public string Name { get; private set; } = "";

    public string Credits { get; private set; } = "";
    
    public Art? CoverArt
    {
        get;
        set
        {
            if (ReferenceEquals(field, value)) return;

            // This model owns the texture once assigned.
            field?.Dispose();

            field = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }    
}