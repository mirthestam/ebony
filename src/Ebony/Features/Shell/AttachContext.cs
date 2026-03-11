using Gio;

namespace Ebony.Features.Shell;

public sealed class AttachContext
{
    public required Action<string, SimpleActionGroup> InsertAppActionGroup { get; set; }
    public required Action<string, string[]> SetAccelsForAction { get; set; }
}