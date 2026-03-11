using System.Text.Json;
using System.Text.Json.Serialization;
using Ebony.Core;
using Ebony.Core.Extraction;

namespace Ebony.Infrastructure.Caching;

public class IdJsonConverter(IEbonyControl ebonyControl) : JsonConverter<Id>
{
    public override Id? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string value for Id");

        var idString = reader.GetString();
        return string.IsNullOrEmpty(idString) ? Id.Empty : ebonyControl.Parse(idString);
    }

    public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
    {
        if (value == Id.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
