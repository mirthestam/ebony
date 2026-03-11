using Ebony.Backends.MPD.Connection.Commands.Find;
using MpcNET.Tags;
using MpcNET.Types;

namespace Ebony.Backends.MPD.Connection.Commands.Search;

public class SearchCommand : BaseFilterCommand
{
    public SearchCommand(ITag tag, string searchText, int windowStart = -1, int windowEnd = -1) : base(tag, searchText,
        windowStart, windowEnd)
    {
    }

    public SearchCommand(List<KeyValuePair<ITag, string>> filters, int windowStart = -1, int windowEnd = -1) : base(
        filters, windowStart, windowEnd)
    {
    }
    
    protected override string CommandName => "search";
}