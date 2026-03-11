using MpcNET.Tags;

namespace Ebony.Backends.MPD.Extraction;

/// <summary>
///     Tags supported by MPD but not yet supported by the used MPD library (MpcNET)
/// </summary>
public static class ExtraMpdTags
{
    public static ITag Conductor { get; } = new ExtraTag("conductor");

    public static ITag Ensemble { get; } = new ExtraTag("ensemble");
    
    private class ExtraTag : ITag
    {
        internal ExtraTag(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}