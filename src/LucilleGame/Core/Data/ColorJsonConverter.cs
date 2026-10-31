using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Data;

public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("r");
        writer.WriteValue(value.R);

        writer.WritePropertyName("g");
        writer.WriteValue(value.G);

        writer.WritePropertyName("b");
        writer.WriteValue(value.B);

        writer.WritePropertyName("a");
        writer.WriteValue(value.A);

        writer.WriteEndObject();
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            JObject obj = JObject.Load(reader);

            float r = obj["r"]?.Value<float>() ?? 0f;
            float g = obj["g"]?.Value<float>() ?? 0f;
            float b = obj["b"]?.Value<float>() ?? 0f;
            float a = obj["a"]?.Value<float>() ?? 1f;

            return new Color(r, g, b, a);
        }

        if (reader.TokenType == JsonToken.StartArray)
        {
            JArray arr = JArray.Load(reader);

            if (arr.Count == 3)
            {
                return new Color(arr[0]!.Value<float>(), arr[1]!.Value<float>(), arr[2]!.Value<float>());
            }

            if (arr.Count == 4)
                return new Color(arr[0]!.Value<float>(), arr[1]!.Value<float>(), arr[2]!.Value<float>(), arr[3]!.Value<float>());

            throw new JsonSerializationException($"Color array must have 3 or 4 elements, got {arr.Count}.");
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Color.");
    }
}
