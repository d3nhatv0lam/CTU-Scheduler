using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Extensions;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.TeachingPlan;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Services.Network;

public class CtuHttpConfigurationTests
{
    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    private static CtuSession CreateDummySession(string jwt = "jwt-token-value", string cookieValue = "sess-123")
    {
        var profile = new StudentProfile(
            "B2000001", "Nguyen Van A", "DI2396A1", "CNTT", "Truong CNTT&TT", 46,
            100, 2026, 2, 20, 10);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        return new CtuSession(
            new DkmhSession(jwt, expiresAt, profile),
            new HtqlSession(new Dictionary<string, string> { { "PHPSESSID", cookieValue } }, expiresAt));
    }

    [Fact]
    public void ApplyBrowserHeaders_CalledMultipleTimes_DoesNotDuplicateHeaders()
    {
        var options = new CtuHttpClientOptions();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dkmh.ctu.edu.vn/");

        options.ApplyBrowserHeaders(request.Headers);
        options.ApplyBrowserHeaders(request.Headers);

        const string expectedAcceptLanguage = "vi-VN, vi; q=0.9, en-US; q=0.8, en; q=0.7";
        request.Headers.UserAgent.ToString().Should().Be(options.UserAgent);
        request.Headers.AcceptLanguage.Should().HaveCount(4);
        request.Headers.AcceptLanguage.ToString().Should().Be(expectedAcceptLanguage);
    }

    [Fact]
    public void AddInfrastructureServices_ConfiguresOnlyCtuHttpClients_LeavingDefaultClientClean()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICtuSessionAccessor>());
        services.AddSingleton(Substitute.For<ICtuSessionStore>());
        services.AddSingleton(Substitute.For<ISessionCoordinator>());
        services.AddInfrastructureServices();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var defaults = new CtuHttpClientOptions();

        using var anonymousClient = factory.CreateClient(CtuHttpClientOptions.AnonymousClientName);
        using var courseClient = factory.CreateClient(nameof(CourseRegistrationClient));
        using var authClient = factory.CreateClient(nameof(AuthClient));
        using var legacyClient = factory.CreateClient(nameof(LegacyAuthClient));
        using var htmlClient = factory.CreateClient(nameof(HtqlHtmlAnnouncementService));
        using var jsonClient = factory.CreateClient(nameof(HtqlJsonAnnouncementService));
        using var pdfClient = factory.CreateClient(nameof(ITeachingPlanPdfService));
        using var externalDefaultClient = factory.CreateClient();

        const string expectedAcceptLanguage = "vi-VN, vi; q=0.9, en-US; q=0.8, en; q=0.7";
        anonymousClient.Timeout.Should().Be(defaults.PingTimeout);
        anonymousClient.DefaultRequestHeaders.UserAgent.ToString().Should().Be(defaults.UserAgent);
        anonymousClient.DefaultRequestHeaders.AcceptLanguage.ToString().Should().Be(expectedAcceptLanguage);

        foreach (var client in new[] { courseClient, authClient, legacyClient, htmlClient, jsonClient, pdfClient })
        {
            client.Timeout.Should().Be(defaults.DefaultTimeout);
            client.DefaultRequestHeaders.UserAgent.ToString().Should().Be(defaults.UserAgent);
            client.DefaultRequestHeaders.AcceptLanguage.ToString().Should().Be(expectedAcceptLanguage);
        }

        externalDefaultClient.DefaultRequestHeaders.UserAgent.Should().BeEmpty();
        externalDefaultClient.DefaultRequestHeaders.AcceptLanguage.Should().BeEmpty();
    }

    [Fact]
    public async Task CtuJwtAuthHandler_OnlySetsAuthorization_WithoutAddingBrowserHeaders()
    {
        var accessor = Substitute.For<ICtuSessionAccessor>();
        accessor.Current.Returns(CreateDummySession("my-jwt-123"));
        HttpRequestMessage? captured = null;

        var handler = new CtuJwtAuthHandler(accessor)
        {
            InnerHandler = new CapturingHandler(req =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://dkmhback.ctu.edu.vn/api"), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "my-jwt-123"));
        captured.Headers.UserAgent.Should().BeEmpty();
        captured.Headers.AcceptLanguage.Should().BeEmpty();
    }

    [Fact]
    public async Task CtuLegacyCookieHandler_ReplacesCookieWithoutAddingBrowserHeaders()
    {
        var accessor = Substitute.For<ICtuSessionAccessor>();
        accessor.Current.Returns(CreateDummySession(cookieValue: "fresh-sess"));
        HttpRequestMessage? captured = null;

        var handler = new CtuLegacyCookieHandler(accessor)
        {
            InnerHandler = new CapturingHandler(req =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://dkmh.ctu.edu.vn/");
        request.Headers.TryAddWithoutValidation("Cookie", "PHPSESSID=stale-sess");

        await invoker.SendAsync(request, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Headers.GetValues("Cookie").Should().ContainSingle().Which.Should().Be("PHPSESSID=fresh-sess");
        captured.Headers.UserAgent.Should().BeEmpty();
        captured.Headers.AcceptLanguage.Should().BeEmpty();
    }

    [Fact]
    public async Task CtuSessionRecoveryHandler_WhenRetryingBufferedJson_PreservesContentLength()
    {
        var initialSession = CreateDummySession("expired-jwt");
        var refreshedSession = CreateDummySession("fresh-jwt");

        var store = Substitute.For<ICtuSessionStore>();
        store.Current.Returns(initialSession);

        var coordinator = Substitute.For<ISessionCoordinator>();
        coordinator.RefreshSessionAsync(initialSession, Arg.Any<CancellationToken>())
            .Returns(refreshedSession);

        int attempt = 0;
        long? retryContentLength = null;
        string? retryContentType = null;

        var recoveryHandler = new CtuSessionRecoveryHandler(
            store,
            coordinator,
            NullLogger<CtuSessionRecoveryHandler>.Instance)
        {
            InnerHandler = new CapturingHandler(req =>
            {
                attempt++;
                if (attempt == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                retryContentLength = req.Content?.Headers.ContentLength;
                retryContentType = req.Content?.Headers.ContentType?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };

        using var invoker = new HttpMessageInvoker(recoveryHandler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dkmhback.ctu.edu.vn/api/v1/test")
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"a\":1}"))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempt.Should().Be(2);
        retryContentLength.Should().Be(7);
        retryContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task CtuSessionRecoveryHandler_WhenLegacyPageRedirectsToRelativeLogin_RefreshesAndRetries()
    {
        var initialSession = CreateDummySession(cookieValue: "expired-sess");
        var refreshedSession = CreateDummySession(cookieValue: "fresh-sess");

        var store = Substitute.For<ICtuSessionStore>();
        store.Current.Returns(initialSession);

        var coordinator = Substitute.For<ISessionCoordinator>();
        coordinator.RefreshSessionAsync(initialSession, Arg.Any<CancellationToken>())
            .Returns(refreshedSession);

        int attempt = 0;
        var recoveryHandler = new CtuSessionRecoveryHandler(
            store,
            coordinator,
            NullLogger<CtuSessionRecoveryHandler>.Instance)
        {
            InnerHandler = new CapturingHandler(_ =>
            {
                attempt++;
                if (attempt == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "<html><script language=\"javascript\">location.href='../../login.php';</script></html>",
                            Encoding.UTF8,
                            "text/html")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "<html><body><form name=\"frmXemDiem\"></form></body></html>",
                        Encoding.UTF8,
                        "text/html")
                };
            })
        };

        using var invoker = new HttpMessageInvoker(recoveryHandler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dkmh.ctu.edu.vn/htql/sinhvien/qldiem/codes/index.php?mID=201");

        using var response = await invoker.SendAsync(request, CancellationToken.None);
        string body = await response.Content.ReadAsStringAsync();

        attempt.Should().Be(2);
        await coordinator.Received(1).RefreshSessionAsync(initialSession, Arg.Any<CancellationToken>());
        body.Should().Contain("frmXemDiem");
    }
}
