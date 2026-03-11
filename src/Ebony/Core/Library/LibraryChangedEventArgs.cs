namespace Ebony.Core.Library;

public class LibraryChangedEventArgs(LibraryChangedFlags flags) : EventArgs
{
    public LibraryChangedFlags Flags { get; } = flags;
}