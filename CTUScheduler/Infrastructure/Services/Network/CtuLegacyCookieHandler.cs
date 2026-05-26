using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.CtuSessions;

namespace CTUScheduler.Infrastructure.Services.Network;

public class CtuLegacyCookieHandler : DelegatingHandler
{
    private readonly ICtuSessionStore _sessionStore;

    public CtuLegacyCookieHandler(ICtuSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

        var currentSession = _sessionStore.CurrentSession;

        if (currentSession is not null &&
            currentSession.LegacyWebCookies.Any())
        {
            var cookieString = currentSession.LegacyWebCookies
                .Select(x => $"{x.Key}={x.Value}");

            request.Headers.Add("Cookie", string.Join("; ", cookieString));
        }

        return base.SendAsync(request, cancellationToken);
    }
}