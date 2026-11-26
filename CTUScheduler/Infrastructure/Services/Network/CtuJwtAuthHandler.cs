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
        var currentSession = _sessionAccessor.Current;

        if (currentSession is not null && !string.IsNullOrEmpty(currentSession.Dkmh.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.Dkmh.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}