namespace Ebony.Core.Library;

public interface IHasAssets
{
    public IReadOnlyCollection<AssetInfo> Assets { get;} 
}