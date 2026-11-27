using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Infrastructure.Services.TeachingPlan;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.TeachingPlan;

public class HtqlAnnouncementServiceTests
{
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    [Fact]
    public void HtmlParser_ValidHtmlWithActiveAndCommentedItems_ExtractsOnlyActiveAndSkipsComments()
    {
        const string html = """
            <html>
            <body>
                <td id="module-thongbao">
                    <ul class="thongbao">
                        <!-- <li><a href="old_notice.pdf">Thông báo cũ bị comment</a></li> -->
                        <!-- <li><a href="old_hk1.pdf">Kế hoạch học tập HK1 2025</a></li> -->
                        <li><a href="KeHoachGiangDayVaDangKyHocphan_HK2_2026_2027.signed.signed.signed.signed.pdf">Kế hoạch giảng dạy và đăng ký học phần HK2 (2026-2027)</a></li>
                        <li><a href="#">Mục chỉ dẫn không có link</a></li>
                        <li><a href="https://example.test/ThongBaoDongWebsite.pdf">Thông báo đóng website kế hoạch học tập</a></li>
                    </ul>
                </td>
            </body>
            </html>
            """;

        var baseUri = new Uri("https://example.test/sub/");
        var result = HtqlHtmlAnnouncementService.ParseAnnouncementsFromHtml(html, baseUri);

        result.Should().HaveCount(2);

        result[0].Title.Should().Be("Kế hoạch giảng dạy và đăng ký học phần HK2 (2026-2027)");
        result[0].Link.Should().Be("https://example.test/sub/KeHoachGiangDayVaDangKyHocphan_HK2_2026_2027.signed.signed.signed.signed.pdf");

        result[1].Title.Should().Be("Thông báo đóng website kế hoạch học tập");
        result[1].Link.Should().Be("https://example.test/ThongBaoDongWebsite.pdf");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html><body><div>No target module here</div></body></html>")]
    public void HtmlParser_EmptyOrInvalidHtml_ReturnsEmptyList(string? html)
    {
        var result = HtqlHtmlAnnouncementService.ParseAnnouncementsFromHtml(html!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void JsonParser_ValidJson_ReturnsDeserializedList()
    {
        const string json = """
            [
                {
                    "noidung": "Kế hoạch giảng dạy HK1 2026-2027",
                    "link": "https://htql.ctu.edu.vn/htql/hk1.pdf"
                }
            ]
            """;

        var result = HtqlJsonAnnouncementService.ParseAnnouncementsFromJson(json);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Kế hoạch giảng dạy HK1 2026-2027");
        result[0].Link.Should().Be("https://htql.ctu.edu.vn/htql/hk1.pdf");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JsonParser_EmptyOrNullJson_ReturnsEmptyList(string? json)
    {
        var result = HtqlJsonAnnouncementService.ParseAnnouncementsFromJson(json!);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregatedService_WhenBothSucceed_MergesAndDeduplicates_PreservingHtmlPriority()
    {
        var htmlContent = """
            <td id="module-thongbao">
                <ul class="thongbao">
                    <li><a href="https://example.test/hk2.pdf">Kế hoạch HK2</a></li>
                    <li><a href="https://example.test/shared.pdf">Tiêu đề bản tin HTML</a></li>
                </ul>
            </td>
            """;

        var jsonContent = """
            [
                { "noidung": "Kế hoạch HK1", "link": "https://example.test/hk1.pdf" },
                { "noidung": "Tiêu đề bản tin JSON", "link": "https://example.test/shared.pdf" }
            ]
            """;

        var htmlHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        });
        var jsonHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        });

        var htmlService = new HtqlHtmlAnnouncementService(new HttpClient(htmlHandler), Substitute.For<ILogger<HtqlHtmlAnnouncementService>>());
        var jsonService = new HtqlJsonAnnouncementService(new HttpClient(jsonHandler), Substitute.For<ILogger<HtqlJsonAnnouncementService>>());
        var aggregatedService = new AggregatedSchoolAnnouncementService(
            htmlService, jsonService, Substitute.For<ILogger<AggregatedSchoolAnnouncementService>>());

        var result = await aggregatedService.FetchAnnouncementsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().HaveCount(3);

        // Thứ tự: HTML được ưu tiên trước, và liên kết trùng thì giữ thông tin từ HTML
        result.Content![0].Link.Should().Be("https://example.test/hk2.pdf");
        result.Content[0].Title.Should().Be("Kế hoạch HK2");

        result.Content[1].Link.Should().Be("https://example.test/shared.pdf");
        result.Content[1].Title.Should().Be("Tiêu đề bản tin HTML");

        result.Content[2].Link.Should().Be("https://example.test/hk1.pdf");
        result.Content[2].Title.Should().Be("Kế hoạch HK1");
    }

    [Fact]
    public async Task AggregatedService_WhenHtmlFailsAndJsonSucceeds_ReturnsJsonList()
    {
        var jsonContent = """
            [
                { "noidung": "Kế hoạch HK1", "link": "https://htql.ctu.edu.vn/htql/hk1.pdf" }
            ]
            """;

        var htmlHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var jsonHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        });

        var htmlService = new HtqlHtmlAnnouncementService(new HttpClient(htmlHandler), Substitute.For<ILogger<HtqlHtmlAnnouncementService>>());
        var jsonService = new HtqlJsonAnnouncementService(new HttpClient(jsonHandler), Substitute.For<ILogger<HtqlJsonAnnouncementService>>());
        var aggregatedService = new AggregatedSchoolAnnouncementService(
            htmlService, jsonService, Substitute.For<ILogger<AggregatedSchoolAnnouncementService>>());

        var result = await aggregatedService.FetchAnnouncementsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().HaveCount(1);
        result.Content![0].Title.Should().Be("Kế hoạch HK1");
    }

    [Fact]
    public async Task AggregatedService_WhenBothFail_ReturnsFailure()
    {
        var htmlHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var jsonHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var htmlService = new HtqlHtmlAnnouncementService(new HttpClient(htmlHandler), Substitute.For<ILogger<HtqlHtmlAnnouncementService>>());
        var jsonService = new HtqlJsonAnnouncementService(new HttpClient(jsonHandler), Substitute.For<ILogger<HtqlJsonAnnouncementService>>());
        var aggregatedService = new AggregatedSchoolAnnouncementService(
            htmlService, jsonService, Substitute.For<ILogger<AggregatedSchoolAnnouncementService>>());

        var result = await aggregatedService.FetchAnnouncementsAsync();

        result.IsSuccess.Should().BeFalse();
        result.FirstErrorMessage.Should().Be("Không thể tải thông báo từ tất cả các nguồn.");
    }

    [Theory]
    [InlineData("<form action=\"/captcha_resp\"><input name=\"captcha_resp_txt\"/></form>")]
    [InlineData("<html><body><img src=\"/captcha.gif\" /></body></html>")]
    public void HtmlParser_WhenCaptchaSignaturePresent_DetectsCaptcha(string html)
    {
        var isCaptcha = HtqlHtmlAnnouncementService.IsCaptchaHtml(html);
        isCaptcha.Should().BeTrue();
    }

    [Fact]
    public async Task HtmlService_WhenCaptchaPageReturned_ReturnsCaptchaBlockedError()
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

        var service = new HtqlHtmlAnnouncementService(new HttpClient(handler), Substitute.For<ILogger<HtqlHtmlAnnouncementService>>());
        var result = await service.FetchAnnouncementsAsync();

        result.IsFailed.Should().BeTrue();
        result.FirstErrorMessage.Should().Be("Kẹt Captcha, hãy bật VPN hoặc đổi mạng");
        result.Errors.Should().Contain(e => e.Code == "TeachingPlan.CaptchaBlocked");
    }

    [Fact]
    public async Task JsonService_WhenCaptchaHtmlReturned_ReturnsCaptchaBlockedError()
    {
        const string captchaHtml = "<html><body><form action=\"/captcha_resp\"><img src=\"/captcha.gif\" /></form></body></html>";

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
        });

        var service = new HtqlJsonAnnouncementService(new HttpClient(handler), Substitute.For<ILogger<HtqlJsonAnnouncementService>>());
        var result = await service.FetchAnnouncementsAsync();

        result.IsFailed.Should().BeTrue();
        result.FirstErrorMessage.Should().Be("Kẹt Captcha, hãy bật VPN hoặc đổi mạng");
        result.Errors.Should().Contain(e => e.Code == "TeachingPlan.CaptchaBlocked");
    }

    [Fact]
    public async Task AggregatedService_WhenBothFailDueToCaptcha_PropagatesCaptchaBlockedError()
    {
        const string captchaHtml = "<html><body><form action=\"/captcha_resp\"><img src=\"/captcha.gif\" /></form></body></html>";

        var htmlHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
        });
        var jsonHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(captchaHtml, Encoding.UTF8, "text/html")
        });

        var htmlService = new HtqlHtmlAnnouncementService(new HttpClient(htmlHandler), Substitute.For<ILogger<HtqlHtmlAnnouncementService>>());
        var jsonService = new HtqlJsonAnnouncementService(new HttpClient(jsonHandler), Substitute.For<ILogger<HtqlJsonAnnouncementService>>());
        var aggregatedService = new AggregatedSchoolAnnouncementService(
            htmlService, jsonService, Substitute.For<ILogger<AggregatedSchoolAnnouncementService>>());

        var result = await aggregatedService.FetchAnnouncementsAsync();

        result.IsFailed.Should().BeTrue();
        result.FirstErrorMessage.Should().Be("Kẹt Captcha, hãy bật VPN hoặc đổi mạng");
        result.Errors.Should().Contain(e => e.Code == "TeachingPlan.CaptchaBlocked");
    }
}
