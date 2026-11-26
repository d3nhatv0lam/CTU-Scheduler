using CTUScheduler.Infrastructure.Sites.CTU.Helpers;
using FluentAssertions;
using HtmlAgilityPack;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class CtuCaptchaDetectorTests
{
    [Theory]
    [InlineData("<form action=\"/captcha_resp\"><input name=\"captcha_resp_txt\"/></form>")]
    [InlineData("<html><body><img src=\"/captcha.gif\" /></body></html>")]
    [InlineData("<html><body><input type=\"text\" name=\"captcha_resp_txt\" /></body></html>")]
    public void IsCaptcha_HtmlWithCaptchaSignature_ReturnsTrue(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var resultFromString = CtuCaptchaDetector.IsCaptcha(html);
        var resultFromDoc = CtuCaptchaDetector.IsCaptcha(doc);

        resultFromString.Should().BeTrue();
        resultFromDoc.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html><body><form id=\"loginForm\"><input name=\"txtDinhDanh\" /></form></body></html>")]
    public void IsCaptcha_NullEmptyOrNormalHtml_ReturnsFalse(string? html)
    {
        var result = CtuCaptchaDetector.IsCaptcha(html);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsCaptcha_NullHtmlDocument_ReturnsFalse()
    {
        HtmlDocument? doc = null;

        var result = CtuCaptchaDetector.IsCaptcha(doc);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("  <html><body><form action=\"/captcha_resp\"></form></body></html>", true)]
    [InlineData("<html><body><img src=\"/captcha.gif\" /></body></html>", true)]
    [InlineData("[{\"noidung\":\"Thông báo /captcha.gif\",\"link\":\"https://htql.ctu.edu.vn/a.pdf\"}]", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsCaptchaResponse_VariousPayloads_DetectsOnlyHtmlCaptcha(string? payload, bool expected)
    {
        var result = CtuCaptchaDetector.IsCaptchaResponse(payload);

        result.Should().Be(expected);
    }
}
