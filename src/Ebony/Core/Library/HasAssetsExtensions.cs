namespace Ebony.Core.Library;

public static class HasAssetsExtensions
{
    extension(IReadOnlyCollection<AssetInfo> assets)
    {
        public AssetInfo? FrontCover => assets.FirstOrDefault(r => r.Type == AssetType.FrontCover);
    }
}