using System;
using System.Net.Http.Headers;

namespace CTUScheduler.Infrastructure.Services.Network;

public sealed class CtuHttpClientOptions
{
    public const string AnonymousClientName = "CtuAnonymous";

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public string AcceptLanguage { get; set; } =
        "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7";

    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public void ApplyBrowserHeaders(HttpRequestHeaders headers)
    {
        if (headers.UserAgent.Count == 0 && !string.IsNullOrWhiteSpace(UserAgent))
        {
            headers.UserAgent.ParseAdd(UserAgent);
        }

        if (headers.AcceptLanguage.Count == 0 && !string.IsNullOrWhiteSpace(AcceptLanguage))
        {
            headers.AcceptLanguage.ParseAdd(AcceptLanguage);
        }
    }
}
