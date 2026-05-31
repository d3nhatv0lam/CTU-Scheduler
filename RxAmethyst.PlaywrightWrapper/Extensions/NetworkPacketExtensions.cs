using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RxAmethyst.PlaywrightWrapper.Models;

namespace RxAmethyst.PlaywrightWrapper.Extensions;

public static class NetworkPacketExtensions
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
    public static IObservable<RawApiBody<T>> ParseResponse<T>(this IObservable<NetworkPacket> source,
        Func<JsonNode, JsonNode?>? nodeSelector = null)
    {
        return source.Select(packet =>
        {
            if (packet is null || string.IsNullOrWhiteSpace(packet.RawBody))
                return new RawApiBody<T> { Content = default };

            try
            {
                var rootNode = JsonNode.Parse(packet.RawBody);
                if (rootNode is null)
                    return new RawApiBody<T> { Content = default };

                var targetNode = nodeSelector != null
                    ? nodeSelector(rootNode)
                    : rootNode;

                var data = targetNode != null
                    ? targetNode.Deserialize<T>()
                    : default;

                return new RawApiBody<T> { Content = data };
            }
            catch
            {
                return new RawApiBody<T>
                {
                    Content = default,
                };
            }
        });
    }
}