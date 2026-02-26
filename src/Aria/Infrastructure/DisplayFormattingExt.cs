namespace Aria.Infrastructure;

public static class DisplayFormattingExt
{
    extension(TimeSpan timeSpan)
    {
        public string ToDisplayString()
        {
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString(@"h\:mm\:ss")
                : timeSpan.ToString(@"mm\:ss");        
        }
    }
}