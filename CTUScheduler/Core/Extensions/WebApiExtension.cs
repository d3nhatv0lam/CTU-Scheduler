using System;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Interfaces.WebDriver.Api;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.Core.Extensions;

public static class WebApiExtension
{
    /// <summary>
    ///  Filter packets by json keys
    /// </summary>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">source is null</exception>
    public static IObservable<NetworkPacket> FilterPacketJson(this IObservable<NetworkPacket> source
        , Func<JsonNode, bool>? predicate = null)
    {
        if (predicate == null) return source;
        return source.Where(packet =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packet.RawBody)) return false;
                var node = JsonNode.Parse(packet.RawBody);
                if (node is null) return false;
                return predicate(node);
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Parses the response from the server into a generic type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="nodeSelector"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">source is null</exception>
    public static IObservable<IApiBody<T>> ParseResponse<T>(this IObservable<NetworkPacket> source,
        Func<JsonNode, JsonNode?>? nodeSelector = null)
    {
        return source.Select(packet =>
        {
            if (packet is null || string.IsNullOrWhiteSpace(packet.RawBody))
                return new RawApiBody<T> { Data = default };

            try
            {
                var rootNode = JsonNode.Parse(packet.RawBody);
                if (rootNode is null) 
                    return new RawApiBody<T> { Data = default };

                var targetNode = nodeSelector != null 
                    ? nodeSelector(rootNode) 
                    : rootNode;
                
                var data = targetNode != null 
                    ? targetNode.Deserialize<T>() 
                    : default;

                return new RawApiBody<T> { Data = data };
            }
            catch
            {
                return new RawApiBody<T>
                {
                    Data = default,
                };
            }
        });
    }

    /// <summary>
    ///  Parses the response from the server into a generic type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="nodeSelector"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">source is null</exception>
    public static IObservable<CtuApiBody<T>> ParseCtuResponse<T>(this IObservable<NetworkPacket> source,
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