using System;
using CTUScheduler.Infrastructure.Services.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CTUScheduler.Infrastructure.Extensions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder ConfigureCtuDefaults(
        this IHttpClientBuilder builder,
        TimeSpan? timeoutOverride = null)
    {
        return builder.ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CtuHttpClientOptions>>().Value;
            options.ApplyBrowserHeaders(client.DefaultRequestHeaders);
            client.Timeout = timeoutOverride ?? options.DefaultTimeout;
        });
    }
}
