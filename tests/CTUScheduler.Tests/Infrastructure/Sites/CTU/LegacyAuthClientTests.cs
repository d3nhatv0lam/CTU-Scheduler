using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Networking;
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

    [Fact]
    public async Task AuthenticateAsync_WhenLegacyLoginSucceeds_WarmsUpStudentHomeBeforeDkmhLogin()
    {
        var requests = new List<(HttpMethod Method, Uri? Uri)>();
        string testJwt = CreateTestJwt("B2303830");

        var pingClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new LegacyAuthClient(
            pingClient,
            NullLogger<LegacyAuthClient>.Instance,
            cookies => new StubHttpMessageHandler(req =>
            {
                requests.Add((req.Method, req.RequestUri));

                if (req.RequestUri == HtqlEndpoints.LegacyLogin)
                {
                    cookies.Add(new Cookie("PHPSESSID", "php-sess-1", "/", HtqlEndpoints.LegacyLogin.Host));
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
                    };
                }

                if (req.RequestUri == HtqlEndpoints.StudentHome)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><body><form name=\"frmDuLieuDindex\"></form></body></html>", Encoding.UTF8, "text/html")
                    };
                }

                if (req.RequestUri == DkmhEndpoints.Login)
                {
                    cookies.Add(DkmhEndpoints.BaseDomain, new Cookie("access_token", testJwt));
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

        var session = await client.AuthenticateAsync("B2303830", "secret");

        requests.Should().Equal(
            (HttpMethod.Post, HtqlEndpoints.LegacyLogin),
            (HttpMethod.Get, HtqlEndpoints.StudentHome),
            (HttpMethod.Post, DkmhEndpoints.Login));
        session.StudentId.Should().Be("B2303830");
        session.Htql.Should().NotBeNull();
        session.Htql!.Cookies.Should().ContainKey("PHPSESSID").WhoseValue.Should().Be("php-sess-1");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenStudentHomeReturnsServerError_ContinuesGracefully()
    {
        string testJwt = CreateTestJwt("B2303830");
        var pingClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new LegacyAuthClient(
            pingClient,
            NullLogger<LegacyAuthClient>.Instance,
            cookies => new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri == HtqlEndpoints.LegacyLogin)
                {
                    cookies.Add(new Cookie("PHPSESSID", "php-sess-1", "/", HtqlEndpoints.LegacyLogin.Host));
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
                    };
                }

                if (req.RequestUri == HtqlEndpoints.StudentHome)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                if (req.RequestUri == DkmhEndpoints.Login)
                {
                    cookies.Add(DkmhEndpoints.BaseDomain, new Cookie("access_token", testJwt));
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

        var session = await client.AuthenticateAsync("B2303830", "secret");

        session.StudentId.Should().Be("B2303830");
        session.Dkmh.AccessToken.Should().Be(testJwt);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenStudentHomeReturnsCaptcha_ThrowsCaptchaBlockedException()
    {
        const string captchaHtml = "<html><body><form action=\"/captcha_resp\"><img src=\"/captcha.gif\" /></form></body></html>";
        var pingClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new LegacyAuthClient(
            pingClient,
            NullLogger<LegacyAuthClient>.Instance,
            _ => new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri == HtqlEndpoints.StudentHome)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
                };
            }));

        var act = () => client.AuthenticateAsync("B2303830", "secret");

        await act.Should().ThrowAsync<CaptchaBlockedException>();
    }

    [Fact]
    public async Task AuthClient_CompleteAuthenticationAsync_WarmsUpStudentHomeAfterSsoPost()
    {
        var requests = new List<(HttpMethod Method, Uri? Uri)>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(req =>
        {
            requests.Add((req.Method, req.RequestUri));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body>OK</body></html>", Encoding.UTF8, "text/html")
            };
        }));

        bool result = await AuthClient.CompleteAuthenticationAsync(
            httpClient,
            new SamlLoginContext("saml-payload"),
            NullLogger.Instance);

        result.Should().BeTrue();
        requests.Should().Equal(
            (HttpMethod.Post, HtqlEndpoints.SsoAuth),
            (HttpMethod.Get, HtqlEndpoints.StudentHome));
    }

    private static string CreateTestJwt(string studentId)
    {
        string profileJson = JsonSerializer.Serialize(new
        {
            sys_manguoidung = studentId,
            sys_hoten = "Test Student",
            sys_malop = "DI23V7A1",
            sys_tennganh = "Kỹ thuật phần mềm",
            sys_tendonvi = "CNTT&TT",
            sys_khoahoc = 49,
            sys_sotinchidat = 100,
            sys_namhocht = 2026,
            sys_hockyht = 1,
            sys_tcmaxhockychinh = 24,
            sys_tcmaxhockyhe = 12
        });

        using var ms = new MemoryStream();
        using (var zlib = new ZLibStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            byte[] profileBytes = Encoding.UTF8.GetBytes(profileJson);
            zlib.Write(profileBytes, 0, profileBytes.Length);
        }

        string compressedUserInfo = Convert.ToBase64String(ms.ToArray());
        string payloadJson = JsonSerializer.Serialize(new
        {
            exp = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(),
            user_info = compressedUserInfo
        });
        string payloadEncoded = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payloadJson));

        return $"eyJhbGciOiJub25lIn0.{payloadEncoded}.sig";
    }
}
