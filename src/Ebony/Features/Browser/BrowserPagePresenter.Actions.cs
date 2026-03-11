using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Features.Browser.Album;
using Ebony.Features.Details;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;

namespace Ebony.Features.Browser;

public partial class BrowserPagePresenter
{
    // Actions
    private SimpleAction _searchAction;
    private SimpleAction _allAlbumsAction;
    private SimpleAction _showArtistAction;
    private SimpleAction _showAlbumAction;
    private SimpleAction _showAlbumForArtistAction;
    private SimpleAction _showPlaylistsAction;

    private SimpleAction _showTrackAction;

    private SimpleAction _runLibraryDiagnosticsAction;

    private SimpleAction _updateLibraryAction;
    
    private void InitializeActions(AttachContext context)
    {
        var diagnosticsActionGroup = SimpleActionGroup.New();
        diagnosticsActionGroup.AddAction(_runLibraryDiagnosticsAction = SimpleAction.New(AppActions.Diagnostics.InspectLibrary.Action, null));
        context.SetAccelsForAction($"{AppActions.Diagnostics.Key}.{AppActions.Diagnostics.InspectLibrary.Action}",
            [AppActions.Diagnostics.InspectLibrary.Accelerator]);        
        context.InsertAppActionGroup(AppActions.Diagnostics.Key, diagnosticsActionGroup);
        
        _runLibraryDiagnosticsAction.OnActivate += RunLibraryDiagnosticsActionOnOnActivate;
        
        var browserActionGroup = SimpleActionGroup.New();

        browserActionGroup.AddAction(_searchAction = SimpleAction.New(AppActions.Browser.Search.Action, null));
        browserActionGroup.AddAction(_allAlbumsAction = SimpleAction.New(AppActions.Browser.ShowAllAlbums.Action, null));
        browserActionGroup.AddAction(_showPlaylistsAction = SimpleAction.New(AppActions.Browser.ShowAllPlaylists.Action, null));        
        browserActionGroup.AddAction(_showArtistAction =
            SimpleAction.New(AppActions.Browser.ShowArtist.Action, GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumAction =
            SimpleAction.New(AppActions.Browser.ShowAlbum.Action, GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumForArtistAction =
            SimpleAction.New(AppActions.Browser.ShowAlbumForArtist.Action,
                GLib.VariantType.NewArray(GLib.VariantType.String)));
        browserActionGroup.AddAction(_showTrackAction =
            SimpleAction.New(AppActions.Browser.ShowTrack.Action, GLib.VariantType.String));
        browserActionGroup.AddAction(_updateLibraryAction =
            SimpleAction.New(AppActions.Browser.Update.Action, null));
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.Search.Action}",
            [AppActions.Browser.Search.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAllAlbums.Action}",
            [AppActions.Browser.ShowAllAlbums.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAllPlaylists.Action}",
            [AppActions.Browser.ShowAllPlaylists.Accelerator]);        
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.Update.Action}",
            [AppActions.Browser.Update.Accelerator]);        
        context.InsertAppActionGroup(AppActions.Browser.Key, browserActionGroup);

        _searchAction.OnActivate += SearchActionOnOnActivate;
        _allAlbumsAction.OnActivate += AllAlbumsActionOnOnActivate;
        _showArtistAction.OnActivate += ShowArtistActionOnOnActivate;
        _showAlbumAction.OnActivate += ShowAlbumActionOnOnActivate;
        _showAlbumForArtistAction.OnActivate += ShowAlbumForArtistActionOnOnActivate;
        _showTrackAction.OnActivate += ShowTrackActionOnOnActivate;
        _showPlaylistsAction.OnActivate += ShowPlaylistsActionOnOnActivate;
        _updateLibraryAction.OnActivate += UpdateLibraryActionOnOnActivate;
    }

    private async void UpdateLibraryActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            _messenger.Send(new ShowToastMessage("Your Music Player is refreshing your library."));
            await _ebony.Library.BeginRefreshAsync();
        }
        catch (Exception ex)
        {
            LogFailedToUpdateLibrary(ex);
        }
    }

    private async void RunLibraryDiagnosticsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ebonyControl.RunInspectionAsync();
        }
        catch (Exception ex)
        {
            LogFailedToRunLibraryDiagnostics(ex);           
        }
    }

    private async void ShowTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var parameter = args.Parameter.GetString(out _);
            var trackId = _ebonyControl.Parse(parameter);

            var trackInfo = (AlbumTrackInfo?)await _ebony.Library.GetItemAsync(trackId);

            if (trackInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this track."));
                return;
            }
            
            var albumInfo = await _ebony.Library.GetAlbumAsync(trackInfo.Track.AlbumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }
            
            var trackDetailsPresenter = _presenterFactory.Create<TrackDetailsDialogPresenter>();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                var dialog = TrackDetailsDialog.NewWithProperties([]);
                trackDetailsPresenter.Attach(dialog);
                trackDetailsPresenter.Load(trackInfo, albumInfo);
                dialog.Present(View);
            });
        }
        catch (Exception e)
        {
            LogFailedToShowTrackDetails(e);           
        }
    }

    private async void ShowAlbumForArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var parameters = args.Parameter.GetStrv(out _);

            var albumId = _ebonyControl.Parse(parameters[0]);
            var artistId = _ebonyControl.Parse(parameters[1]);

            var albumInfo = await _ebony.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }

            var artistInfo = await _ebony.Library.GetArtistAsync(artistId);
            if (artistInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this artist."));
                return;
            }

            _albumPagePresenter = _presenterFactory.Create<AlbumPagePresenter>();

            await GtkDispatch.InvokeIdleAsync(() => 
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return;

                _albumPagePresenter.Attach(albumPageView);

                _ = _albumPagePresenter.LoadAsync(albumInfo, artistInfo);
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void ShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var serializedId = args.Parameter.GetString(out _);
            var albumId = _ebonyControl.Parse(serializedId);

            var albumInfo = await _ebony.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }

            _albumPagePresenter = _presenterFactory.Create<AlbumPagePresenter>();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return;
                _albumPagePresenter.Attach(albumPageView);
            });
            
            await _albumPagePresenter.LoadAsync(albumInfo);
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void ShowArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var serializedId = args.Parameter.GetString(out _);
            var artistId = _ebonyControl.Parse(serializedId);
            LogShowingArtistDetailsForArtist(artistId);

            await _artistsPagePresenter.SelectArtist(artistId);
            await _artistPagePresenter.LoadArtistAsync(artistId);

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.ShowArtistDetailRoot();
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void AllAlbumsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await ShowAllAlbumsAsync();
        }
        catch (Exception ex)
        {
            LogFailedToShowAllAlbums(ex);
        }
    }

    private async void ShowPlaylistsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await ShowPlaylistsAsync();
        }
        catch (Exception ex)
        {
            LogFailedToShowPlaylists(ex);
        }
    }    
    
    private void SearchActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // User wants to start the search functionality
        View?.StartSearch();
    }

    [LoggerMessage(LogLevel.Error, "Failed to parse artist id")]
    partial void LogFailedToParseArtistId(Exception e);

    [LoggerMessage(LogLevel.Debug, "Showing artist details for artist {artistId}")]
    partial void LogShowingArtistDetailsForArtist(Id artistId);

    [LoggerMessage(LogLevel.Error, "Failed to update library")]
    partial void LogFailedToUpdateLibrary(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to run library diagnostics")]
    partial void LogFailedToRunLibraryDiagnostics(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to show track details")]
    partial void LogFailedToShowTrackDetails(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to show all albums")]
    partial void LogFailedToShowAllAlbums(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to show playlists")]
    partial void LogFailedToShowPlaylists(Exception ex);
}