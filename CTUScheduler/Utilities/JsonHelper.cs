using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Utilities
{
    public static class JsonHelper
    {
        public static string Serialize<T>(T obj) 
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
        public static string Serialize<T>(T obj, JsonSerializerOptions options)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj,options);
        }

        public static T? Deserialize<T>(string json) 
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                throw;
            }
            
        }
        public static T? Deserialize<T>(string json,JsonSerializerOptions options)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
            }
            catch
            {
                throw;
            }
        }
        public static T? Deserialize<T>(JsonElement json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                throw;
            }
        }

        public static T? Deserialize<T>(JsonElement json, JsonSerializerOptions options)
        {   
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
            }
            catch
            {
                throw;
            }
        }

    }
}
