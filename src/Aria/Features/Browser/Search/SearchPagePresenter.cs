using Aria.Core;
using Aria.Core.Library;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Search;

public partial class SearchPagePresenter(ILogger<SearchPagePresenter> logger, IAria aria)
{
    private SearchPage View { get; set; } = null!;

    private SearchResults? _searchResults;
    
    private CancellationTokenSource? _searchCts;

    public void Attach(SearchPage view)
    {
        View = view;
        view.SearchChanged += ViewOnSearchChanged;
    }
    
    private void ViewOnSearchChanged(object? sender, string e)
    {
        // use the library to search
        // give the view new search results
        
        _ = Search(e);
    }

    private async Task Search(string searchTerm)
    {
        Abort();
        
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            _searchResults = await aria.Library.SearchAsync(searchTerm, token);
            
            GLib.Functions.TimeoutAdd(0, 0, () =>
            {
                if (token.IsCancellationRequested) return false;
                    
                View.ShowResults(_searchResults);
                return false;
            });                        
        }
        catch (Exception e)
        {
            LogCouldNotSearchForSearchterm(searchTerm, e);           
        }
    }

    private void Abort()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;        
    }

    private void AbortAndClear()
    {
        Abort();
        View.Clear();
    }

    public void Reset()
    {
        LogResettingSearchPage();
        AbortAndClear();
    }

    [LoggerMessage(LogLevel.Debug, "Resetting search page")]
    partial void LogResettingSearchPage();

    [LoggerMessage(LogLevel.Error, "Could not search for {searchTerm}")]
    partial void LogCouldNotSearchForSearchterm(string searchTerm, Exception ex);
}