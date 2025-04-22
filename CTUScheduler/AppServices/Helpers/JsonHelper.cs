using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers
{
    public static class JsonHelper
    {
        public static JsonElement ChangeRoot(JsonElement json, string propertyName)
        {
            return json.GetProperty(propertyName);
        }

        // ---- Serialization ----
        public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(obj, options ?? JsonSerializerOptions.Default);
        }

        // ---- Deserialization ----
        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<T>(json, options ?? JsonSerializerOptions.Default);
        }

        public static T? Deserialize<T>(Stream jsonStream, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<T>(jsonStream, options ?? JsonSerializerOptions.Default);
        }

        public static T? Deserialize<T>(JsonElement jsonData, JsonSerializerOptions? options = null)
        {
            return jsonData.Deserialize<T>(options ?? JsonSerializerOptions.Default);
        }

        // ---- File Operations ----
        public static T? DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null)
        {
            using var stream = File.OpenRead(filePath);
            return Deserialize<T>(stream, options);
        }
    }
}
