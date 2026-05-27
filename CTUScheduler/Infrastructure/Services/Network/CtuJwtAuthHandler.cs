using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.CtuSessions;

namespace CTUScheduler.Infrastructure.Services.Network;

public class CtuJwtAuthHandler : DelegatingHandler
{
    private readonly ICtuSessionAccessor _sessionAccessor;

    public CtuJwtAuthHandler(ICtuSessionAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

        var currentSession = _sessionAccessor.Current;

        if (currentSession is not null && !string.IsNullOrEmpty(currentSession.DkmhApiJwtToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.DkmhApiJwtToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}