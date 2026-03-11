using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Core.Queue;

namespace Ebony.Core;

public interface IEbony
{
    public IPlayer Player { get; }

    public IQueue Queue { get; }

    public ILibrary Library { get; }
}