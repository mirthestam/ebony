using MpcNET.Tags;
using MpcNET.Types;

namespace Ebony.Backends.MPD.Connection.Commands.Find;

public class FindCommand : BaseFilterCommand
{
    public FindCommand(IFilter filter, int windowStart = -1, int windowEnd = -1) : base(filter, windowStart, windowEnd)
    {
    }
    public FindCommand(List<IFilter> filters, int windowStart = -1, int windowEnd = -1) : base(filters, windowStart,
        windowEnd)
    {
    }

    protected override string CommandName => "find";
}