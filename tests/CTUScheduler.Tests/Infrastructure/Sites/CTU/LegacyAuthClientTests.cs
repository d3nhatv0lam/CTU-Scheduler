using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class LegacyAuthClientTests
{
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    [Fact]
    public async Task PingAuthServerAsync_WhenCaptchaPageReturned_ReturnsCaptchaBlocked()
    {
        const string captchaHtml = """
            <html>
            <body>
                <form action="/captcha_resp" method="post">
                    <img src="/captcha.gif" />
                    <input type="text" name="captcha_resp_txt" />
                </form>
            </body>
            </html>
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
        });
        var client = new LegacyAuthClient(new HttpClient(handler), NullLogger<LegacyAuthClient>.Instance);

        var status = await client.PingAuthServerAsync();

        status.Should().Be(AuthServerStatus.CaptchaBlocked);
    }

    [Fact]
    public async Task PingAuthServerAsync_WhenSuccessWithoutCaptcha_ReturnsHealthy()
    {
        const string redirectHtml = "<html><head><meta http-equiv=\"refresh\" content=\"0;url=...\" /></head></html>";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(redirectHtml, Encoding.UTF8, "text/html")
        });
        var client = new LegacyAuthClient(new HttpClient(handler), NullLogger<LegacyAuthClient>.Instance);

        var status = await client.PingAuthServerAsync();

        status.Should().Be(AuthServerStatus.Healthy);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, AuthServerStatus.Maintenance)]
    [InlineData(HttpStatusCode.BadGateway, AuthServerStatus.Maintenance)]
    [InlineData(HttpStatusCode.NotFound, AuthServerStatus.Unreachable)]
    public async Task PingAuthServerAsync_WhenHttpErrorReturned_ReturnsExpectedStatus(
        HttpStatusCode httpStatus,
        AuthServerStatus expected)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(httpStatus));
        var client = new LegacyAuthClient(new HttpClient(handler), NullLogger<LegacyAuthClient>.Instance);

        var status = await client.PingAuthServerAsync();

        status.Should().Be(expected);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenLoginPostReturnsCaptcha_ThrowsCaptchaBlockedException()
    {
        const string captchaHtml = "<html><body><form action=\"/captcha_resp\"><img src=\"/captcha.gif\" /></form></body></html>";
        var pingClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new LegacyAuthClient(
            pingClient,
            NullLogger<LegacyAuthClient>.Instance,
            _ => new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
            }));

        var act = () => client.AuthenticateAsync("B2000001", "secret");

        await act.Should().ThrowAsync<CaptchaBlockedException>();
    }

    [Fact]
    public async Task OrchestratorPingAuthServerAsync_WhenSsoCaptchaBlockedAndLegacyUnreachable_PreservesCaptchaBlocked()
    {
        var ssoCaptchaHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<form action=\"/captcha_resp\"></form>", Encoding.UTF8, "text/html")
        });
        var legacyUnreachableHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(ssoCaptchaHandler));

        var ssoClient = new AuthClient(
            httpClientFactory,
            new HttpClient(ssoCaptchaHandler),
            NullLogger<AuthClient>.Instance);
        var legacyClient = new LegacyAuthClient(
            new HttpClient(legacyUnreachableHandler),
            NullLogger<LegacyAuthClient>.Instance);
        var orchestrator = new AuthClientOrchestrator(
            ssoClient,
            legacyClient,
            NullLogger<AuthClientOrchestrator>.Instance);

        var status = await orchestrator.PingAuthServerAsync();

        status.Should().Be(AuthServerStatus.CaptchaBlocked);
    }
}
