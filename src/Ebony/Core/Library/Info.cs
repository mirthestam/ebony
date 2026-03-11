using Ebony.Core.Extraction;

namespace Ebony.Core.Library;

public abstract record Info
{
    public required Id Id { get; init; }    
}
