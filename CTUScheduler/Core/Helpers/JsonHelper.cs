using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Helpers
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
            return JsonSerializer.Serialize(obj, options);
        }

        // ---- Deserialization ----
        public static T? Deserialize<T>(string jsonString, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<T>(jsonString, options);
        }

        public static T? Deserialize<T>(Stream jsonStream, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<T>(jsonStream, options);
        }

        public static T? Deserialize<T>(JsonElement jsonData, JsonSerializerOptions? options = null)
        {
            return jsonData.Deserialize<T>(options);
        }

        // ---- File Operations ----
        public static T? DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null)
        {
            using var stream = File.OpenRead(filePath);
            return Deserialize<T>(stream, options);
        }
        
        // ---- Serialization (Async) ----
        public static async Task SerializeAsync<T>(Stream stream, T obj, JsonSerializerOptions? options = null)
        {
            await JsonSerializer.SerializeAsync(stream, obj, options);
        }

        public static async Task SerializeToFileAsync<T>(string filePath, T obj, JsonSerializerOptions? options = null)
        {
            await using var stream = File.Create(filePath);
            await SerializeAsync(stream, obj, options);
        }

        // ---- Deserialization (Async) ----
        public static async Task<T?> DeserializeAsync<T>(Stream jsonStream, JsonSerializerOptions? options = null)
        {
            return await JsonSerializer.DeserializeAsync<T>(jsonStream, options);
        }

        public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, JsonSerializerOptions? options = null)
        {
            await using var stream = File.OpenRead(filePath);
            return await DeserializeAsync<T>(stream, options);
        }
    }
}
