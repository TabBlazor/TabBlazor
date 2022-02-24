using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IconGenerator.Converters
{

    public class DictinaryListConverter<T> : JsonConverter<IEnumerable<T>>
    {
        public override IEnumerable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<T>();
            _ = JsonDocument.TryParseValue(ref reader, out JsonDocument doc);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                var name = property.Name;
                var newItem = JsonSerializer.Deserialize<T>(property.Value.GetRawText(), options);
                result.Add(newItem);
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class StringDictinaryConverter<T> : JsonConverter<Dictionary<string, T>>
    {
        public override Dictionary<string, T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<string, T>();
            _ = JsonDocument.TryParseValue(ref reader, out JsonDocument doc);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                var name = property.Name;
                dictionary.Add(name, JsonSerializer.Deserialize<T>(property.Value.GetRawText(), options));
            }
            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, T> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
