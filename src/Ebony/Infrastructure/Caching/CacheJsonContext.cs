using System.Text.Json;
using System.Text.Json.Serialization;
using Ebony.Core;

namespace Ebony.Infrastructure.Caching;

public class CacheJsonContext(IEbonyControl ebonyControl)
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters =
        {
            new IdJsonConverter(ebonyControl)
        }
    };

    public JsonSerializerOptions Options => _options;
}
