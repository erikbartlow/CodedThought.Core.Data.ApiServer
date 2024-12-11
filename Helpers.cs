using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CodedThought.Core.Data.ApiServer
{
    public static class Helpers
    {
        public static JsonSerializerOptions DefaultSerializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
        };

        public static T Deserialize<T>(string content, JsonSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(content);
            options ??= DefaultSerializationOptions;
            var obj = JsonSerializer.Deserialize<T>(content, options);
            return obj;
        }

        public static IList<T> DeserializeList<T>(string content, JsonSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(content);
            options ??= DefaultSerializationOptions;
            List<T> list = JsonSerializer.Deserialize<List<T>>(content, options);
            return [.. list];
        }

        public static string Serialize<T>(T obj, JsonSerializerOptions options = null)
        {
            options ??= DefaultSerializationOptions;
            return JsonSerializer.Serialize<T>(obj, options);
        }

        public static string SerializeList<T>(List<T> list, JsonSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(list);
            options ??= DefaultSerializationOptions;
            return JsonSerializer.Serialize<List<T>>(list, options);
        }
    }
}
