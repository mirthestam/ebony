using System.Globalization;

namespace Ebony.Infrastructure.Extraction;

public static class DateTagParser
{
    public static DateTime? ParseDate(string dateTag)
    {
        if (string.IsNullOrWhiteSpace(dateTag)) return null;
        
        string[] formats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];

        if (DateTime.TryParseExact(dateTag, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        return null;
    }
}