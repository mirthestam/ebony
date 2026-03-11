using MpcNET;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;

namespace Ebony.Backends.MPD.Connection.Commands;

public abstract class BaseFilterCommand : IMpcCommand<IEnumerable<KeyValuePair<string, string>>>
{
    private readonly int _end;
    private readonly List<IFilter> _filters;
    private readonly int _start;

    public string Serialize()
    {
        var serializedFilters = string.Join(" AND ",
                _filters.Select(x => $"{x.GetFormattedCommand()}")
            );
            var cmd = $"""
                       {CommandName} "({serializedFilters})"
                       """;

        if (_start > -1) cmd += $" window {_start}:{_end}";

        return cmd;
    }

    public IEnumerable<KeyValuePair<string, string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues;
    }    
    
    protected BaseFilterCommand(ITag tag, string searchText, int windowStart = -1, int windowEnd = -1,
        FilterOperator operand = FilterOperator.Equal)
    {
        _filters = [];
        var filterTag = new FilterTag(tag, searchText, operand);
        _filters.Add(filterTag);

        _start = windowStart;
        _end = windowEnd;
    }

    protected BaseFilterCommand(List<KeyValuePair<ITag, string>> filters, int windowStart = -1, int windowEnd = -1,
        FilterOperator operand = FilterOperator.Equal)
    {
        _filters = [];
        _filters.AddRange(filters.Select(filter => new FilterTag(filter.Key, filter.Value, operand)).ToList());

        _start = windowStart;
        _end = windowEnd;
    }

    protected BaseFilterCommand(IFilter filters, int windowStart = -1, int windowEnd = -1)
    {
        _filters =
        [
            filters
        ];

        _start = windowStart;
        _end = windowEnd;
    }

    protected BaseFilterCommand(List<IFilter> filters, int windowStart = -1, int windowEnd = -1)
    {
        _filters = filters;

        _start = windowStart;
        _end = windowEnd;
    }

    protected abstract string CommandName { get; }
    
}