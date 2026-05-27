using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static class CtuHttpExtensions
{
    public static async Task<T?> ReadCtuContentAsync<T>(
        this HttpContent content, 
        Func<JsonNode, JsonNode?>? nodeSelector = null, 
        CancellationToken ct = default)
    {
        var rootNode = await content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        if (rootNode is null) return default;
        var code = (int?)rootNode["code"] ?? 500;
        var msg = (string?)rootNode["msg"] ?? "Unknown Error";
            
        // 2. Kiểm tra mã lỗi nghiệp vụ tập trung
        if (code != 200)
        {
            // phải check lại cái này, trường CTU custom
            // 4012 là k hợp lệ
            if (code is 4012)
            {
                throw new SessionExpiredException(msg);
            }
            throw new CtuApiException($"Lỗi từ hệ thống CTU: {msg} (Mã lỗi: {code})");
        }
        
        var targetNode = nodeSelector != null 
            ? nodeSelector(rootNode) 
            : rootNode["data"];
        
        if (targetNode is null)
        {
            return default;
        }
        
        // CTU đôi khi trả data = [] thay vì object/null.
        // Nếu T không phải collection thì coi như không có dữ liệu.
        
        if (targetNode is JsonArray && 
            !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)) && 
            typeof(T) != typeof(string))
        {
            return default;
        }

        return targetNode.Deserialize<T>();
    }
}