using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class CourseRegistrationClientTests
{
    private sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : string.Empty;
            return handler(request, body);
        }
    }

    [Fact]
    public async Task GetTuitionFeeRawAsync_SendsBufferedJsonWithContentLengthAndExactContentType()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        const string okJson = """
            {
                "code": 200,
                "msg": "OK",
                "data": {
                    "tt_tinh_hocphi": "04/01/2027",
                    "chitiethocphi": [],
                    "ghi_chu": { "colLeft": [], "colRight": [] },
                    "thongtin3": []
                }
            }
            """;

        var handler = new CapturingHttpMessageHandler((req, body) =>
        {
            capturedRequest = req;
            capturedBody = body;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(okJson, Encoding.UTF8, "application/json")
            };
        });
        var client = new CourseRegistrationClient(new HttpClient(handler));

        var payload = await client.GetTuitionFeeRawAsync(2026, 2);

        payload.ThongTinTinhHocPhi.Should().Be("04/01/2027");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.Headers.Accept.ToString().Should().Contain("application/json");
        capturedRequest.Content!.Headers.ContentLength.Should().BeGreaterThan(0);
        capturedRequest.Content.Headers.ContentType!.ToString().Should().Be("application/json");
        capturedBody.Should().Be("{\"dkmh_tu_dien_nien_khoa_nam_hoc\":2026,\"dkmh_tu_dien_hoc_ky_ma\":2}");
    }

    [Fact]
    public async Task GetAutoCompleteQueryAsync_SendsBufferedJsonWithContentLengthAndExactContentType()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        const string okJson = """
            {
                "code": 200,
                "msg": "OK",
                "data": {
                    "dkmh_tu_dien_hoc_phan_ma_auto_complete": []
                }
            }
            """;

        var handler = new CapturingHttpMessageHandler((req, body) =>
        {
            capturedRequest = req;
            capturedBody = body;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(okJson, Encoding.UTF8, "application/json")
            };
        });
        var client = new CourseRegistrationClient(new HttpClient(handler));

        var items = await client.GetAutoCompleteQueryAsync("CT101");

        items.Should().BeEmpty();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.Headers.Accept.ToString().Should().Contain("application/json");
        capturedRequest.Content!.Headers.ContentLength.Should().BeGreaterThan(0);
        capturedRequest.Content.Headers.ContentType!.ToString().Should().Be("application/json");
        capturedBody.Should().Contain("\"dkmh_tu_dien_hoc_phan_ma\":\"CT101\"");
    }
}
