using Adw;
using Ebony.Core;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using GLib;
using GObject;
using Gtk;

namespace Ebony.Features.Browser.Album;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(CreditBox)}.ui")]
public partial class CreditBox
{
    [Connect("released-box")] private Box _releasedBox;    
    [Connect("main-box")] private Box _featuringBox;
    [Connect("composers-box")] private Box _composersBox;
    [Connect("arranged-box")] private Box _arrangedBox;
    [Connect("conductors-box")] private Box _conductorsBox;
    [Connect("performers-box")] private Box _performersBox;
    [Connect("solists-box")] private Box _solistsBox;

    [Connect("released-label")] private Label _releasedLabel;    
    [Connect("main-label")] private Label _featuringLabel;
    [Connect("composers-label")] private Label _composersLabel;
    [Connect("arranged-label")] private Label _arrangedLabel;
    [Connect("conductors-label")] private Label _conductorsLabel;
    [Connect("performers-label")] private Label _performersLabel;
    [Connect("solists-label")] private Label _solistsLabel;

    public void UpdateReleasedBox(DateTimeOffset? releaseDate)
    {
        while (_releasedBox.GetFirstChild() != null)
        {
            _releasedBox.Remove(_releasedBox.GetFirstChild()!);
        }

        if (releaseDate == null)
        {
            _releasedLabel.Visible = false;
            _releasedBox.Visible = false;
            return;
        }
        
        _releasedLabel.Visible = true;
        _releasedBox.Visible = true;
        
        var button = Button.NewWithLabel(releaseDate.Value.ToString("yyyy"));
        button.SetCanShrink(true);
        button.AddCssClass("flat");
        button.AddCssClass("artist-link");
        button.AddCssClass("artist-link-common");
        
        _releasedBox.Append(button);
    }
    
    public void UpdateTracksCredits(IList<TrackArtistInfo> artists)
    {
        // Do not remove Album Artists. In some tagging schemes, all artists are listed there.
        // Therefore, this is not a reliable source to determine whether they have already been shown.
        // A better approach would be to check whether the artists are shared across all tracks on the album.
        FillArtistBox(_composersBox, _composersLabel, artists, ArtistRoles.Composer, "Composer", "Composers");
        FillArtistBox(_conductorsBox, _conductorsLabel, artists, ArtistRoles.Conductor, "Conductor", "Conductors");
        FillArtistBox(_arrangedBox, _arrangedLabel, artists, ArtistRoles.Arranger, "Arranger", "Arrangers");
        FillArtistBox(_solistsBox, _solistsLabel, artists, ArtistRoles.Soloist, "Soloist", "Soloists");
        FillArtistBox(_performersBox, _performersLabel, artists, ArtistRoles.Ensemble | ArtistRoles.Performer, "Performer", "Performers");
        FillArtistBox(_featuringBox, _featuringLabel, artists, ArtistRoles.Unknown, "Featuring", "Featuring", true);
    }

    private void FillArtistBox(Box container, Label label, IList<TrackArtistInfo> artists, ArtistRoles roleFilter, string single, string plural,
        bool exact = false)
    {
        while (container.GetFirstChild() != null)
        {
            container.Remove(container.GetFirstChild()!);
        }

        var filteredArtists = artists
            .OrderByDescending(a => a.IsFeatured)
            .ThenBy(a => a.Artist.Name)
            .ThenBy(a => (a.Roles & ArtistRoles.Composer) == 0) // In combined lists, prio composer over others
            .ThenBy(a => (a.Roles & ArtistRoles.Soloist) == 0) // And then ensembles
            .ThenBy(a => (a.Roles & ArtistRoles.Ensemble) == 0); // And then ensembles

        var artistList = filteredArtists.Where(a => exact ? a.Roles == roleFilter : (a.Roles & roleFilter) != 0)
            .ToList();

        label.Visible = artistList.Count > 0;
        container.Visible = artistList.Count > 0;
        
        label.SetText(artistList.Count == 1 ? single : plural);

        const int moreThreshold = 3;
        const int moreMinimum = 3;

        var added = 0;
        foreach (var artist in artistList)
        {
            added++;

            if (added == moreThreshold + 1 && artistList.Count - moreThreshold > moreMinimum)
            {
                var parentContainer = container;
                var expander = Expander.New("More (" + (artistList.Count - moreThreshold) + ")");
                container = New(Orientation.Vertical, 2);
                expander.SetChild(container);
                parentContainer.Append(expander);
            }

            container.Append(CreateArtistBox(artist));
        }
    }

    private static Box CreateArtistBox(TrackArtistInfo artist)
    {
        var box = New(Orientation.Horizontal, 2);
        box.Append(CreateArtistButton(artist));
        if (artist.AdditionalInformation == null) return box;

        var additionalInfoLabel = Label.New($"({artist.AdditionalInformation})");
        additionalInfoLabel.AddCssClass(AdwStyles.Dimmed);
        box.Append(additionalInfoLabel);

        return box;
    }

    private static Button CreateArtistButton(TrackArtistInfo artist)
    {
        // Format the button
        var displayText = artist.Artist.Name;
        var button = Button.NewWithLabel(displayText);
        button.SetCanShrink(true);
        button.AddCssClass("flat");
        button.AddCssClass("artist-link");
        button.AddCssClass(AdwStyles.Colors.Accent);
        
        if (!artist.IsFeatured) button.AddCssClass("artist-link-common");

        // Configure the action
        button.SetActionName($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}");
        var value = Variant.NewString(artist.Artist.Id.ToString());
        button.SetActionTargetValue(value);

        return button;
    }
}