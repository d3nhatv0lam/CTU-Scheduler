using System;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using CTUScheduler.Infrastructure.DriverCore.Response;
using CTUScheduler.Infrastructure.Sites.CTU.Response;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static class CtuNetworkExtensions
{
    /// <summary>
    ///  Parses the response from the server into a generic type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="nodeSelector"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">source is null</exception>
    public static IObservable<IApiBody<T>> ParseCtuResponse<T>(this IObservable<NetworkPacket> source,
        Func<JsonNode, JsonNode?>? nodeSelector = null)
    {
        return source.Select(packet =>
        {
            var failResult = new CtuApiBody<T>
            {
                Code = 500, 
                Message = "Internal Server Error",
                Data = default
            };
            if (packet is null || string.IsNullOrWhiteSpace(packet.RawBody))
                return failResult;

            try
            {
                var rootNode = JsonNode.Parse(packet.RawBody);
                if (rootNode is null) return failResult;
                
                var code = (int?)rootNode["code"] ?? 500;
                var msg = (string?)rootNode["msg"] ?? "Unknown Error";
                
                var targetNode = nodeSelector != null 
                    ? nodeSelector(rootNode) 
                    : rootNode["data"];
                
                var data = targetNode != null 
                    ? targetNode.Deserialize<T>() 
                    : default;
                
                if (data is null)
                    return failResult;
                return new CtuApiBody<T>
                {
                    Code = code, 
                    Message = msg, 
                    Data = data
                };
            }
            catch
            {
                return failResult;
            }
        });
    }
}