using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.CtuSessions;

namespace CTUScheduler.Infrastructure.Services.Network;

public class CtuLegacyCookieHandler : DelegatingHandler
{
    private readonly ICtuSessionAccessor _sessionAccessor;

    public CtuLegacyCookieHandler(ICtuSessionAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var currentSession = _sessionAccessor.Current;

        if (currentSession?.Htql is not null &&
            currentSession.Htql.Cookies.Any())
        {
            var cookieString = currentSession.Htql.Cookies
                .Select(x => $"{x.Key}={x.Value}");

            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookieString));
        }

        return base.SendAsync(request, cancellationToken);
    }
}