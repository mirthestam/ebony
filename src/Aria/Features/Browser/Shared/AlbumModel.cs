using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Shared;

[Subclass<Object>]
public partial class AlbumModel : INotifyPropertyChanged
{
    public static AlbumModel NewForAlbum(AlbumInfo album)
    {
        ArgumentNullException.ThrowIfNull(album);

        var model = NewWithProperties([]);
        model.AlbumId = album.Id;
        model.Title = album.Title;
        model.ReleaseDate = album.ReleaseDate;
        model.CoverArtId = album.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)?.Id;
        model.Credits = string.Join(", ", album.CreditsInfo.AlbumArtists.Select(a => a.Artist.Name));
        
        return model;
    }
    
    public Id AlbumId { get; private set; }
    
    public Id? CoverArtId { get; private set; }
    
    public string Title { get; private set; }
    
    public string Credits { get; private set; }
    
    public DateTime? ReleaseDate { get; private set; }
    
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