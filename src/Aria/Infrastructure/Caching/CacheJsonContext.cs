using System.Text.Json;
using System.Text.Json.Serialization;
using Aria.Core;

namespace Aria.Infrastructure.Caching;

public class CacheJsonContext(IAriaControl ariaControl)
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters =
        {
            new IdJsonConverter(ariaControl)
        }
    };

    public JsonSerializerOptions Options => _options;
}
