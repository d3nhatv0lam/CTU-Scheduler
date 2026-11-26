using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Network;

public sealed class HttpDebugTracingHandler : DelegatingHandler
{
    private readonly ILogger<HttpDebugTracingHandler> _logger;

    public HttpDebugTracingHandler(ILogger<HttpDebugTracingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var contentLengthBeforeSend = request.Content?.Headers.ContentLength;
        var requestHeadersText = FormatHeaders(request.Headers, request.Content?.Headers);

        _logger.LogDebug(
            "HTTP Request => {Method} {Uri} (HTTP/{Version}) | ContentLength={ContentLength} | Headers: {Headers}",
            request.Method,
            request.RequestUri,
            request.Version,
            contentLengthBeforeSend?.ToString() ?? "<null (chunked if body present)>",
            requestHeadersText);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode || _logger.IsEnabled(LogLevel.Debug))
        {
            var requestBody = await TryReadContentAfterSendAsync(request.Content, cancellationToken)
                .ConfigureAwait(false);

            string responseBody = string.Empty;
            if (!response.IsSuccessStatusCode && response.Content is not null)
            {
                // Buffer first so downstream handlers/readers can still read the stream if needed.
                await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            var responseHeadersText = FormatHeaders(response.Headers, response.Content?.Headers);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HTTP Non-Success <= {StatusCode} ({ReasonPhrase}) for {Method} {Uri} (HTTP/{Version}) | ReqContentLength={ReqContentLength} | ReqHeaders={ReqHeaders} | ReqBody={ReqBody} | ResHeaders={ResHeaders} | ResBody={ResBody}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    request.Method,
                    request.RequestUri,
                    request.Version,
                    contentLengthBeforeSend?.ToString() ?? "<null (chunked)>",
                    requestHeadersText,
                    requestBody,
                    responseHeadersText,
                    responseBody);
            }
            else
            {
                _logger.LogDebug(
                    "HTTP Response <= {StatusCode} for {Method} {Uri} | ReqBody={ReqBody}",
                    (int)response.StatusCode,
                    request.Method,
                    request.RequestUri,
                    requestBody);
            }
        }

        return response;
    }

    private static async Task<string> TryReadContentAfterSendAsync(
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return "<empty>";
        }

        try
        {
            return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            return $"<unreadable: {ex.GetType().Name}>";
        }
    }

    private static string FormatHeaders(HttpHeaders primary, HttpContentHeaders? secondary)
    {
        var entries = new List<string>();

        foreach (var header in primary)
        {
            entries.Add($"{header.Key}: {SanitizeHeaderValue(header.Key, header.Value)}");
        }

        if (secondary is not null)
        {
            foreach (var header in secondary)
            {
                entries.Add($"{header.Key}: {SanitizeHeaderValue(header.Key, header.Value)}");
            }
        }

        return entries.Count == 0 ? "<none>" : string.Join("; ", entries);
    }

    private static string SanitizeHeaderValue(string name, IEnumerable<string> values)
    {
        var joined = string.Join(", ", values);

        if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            var preview = joined.Length > 24 ? $"{joined[..24]}..." : joined;
            return $"{preview} (len={joined.Length})";
        }

        if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
        {
            return $"<redacted len={joined.Length}>";
        }

        return joined;
    }
}
