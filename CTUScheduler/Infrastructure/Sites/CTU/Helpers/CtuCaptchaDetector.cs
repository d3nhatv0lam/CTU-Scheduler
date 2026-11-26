using System;
using HtmlAgilityPack;

namespace CTUScheduler.Infrastructure.Sites.CTU.Helpers;

public static class CtuCaptchaDetector
{
    public const string CaptchaXPath =
        "//form[@action='/captcha_resp'] | //input[@name='captcha_resp_txt'] | //img[@src='/captcha.gif']";

    public static bool IsCaptcha(HtmlDocument? doc)
    {
        if (doc?.DocumentNode is null)
            return false;

        return doc.DocumentNode.SelectSingleNode(CaptchaXPath) is not null;
    }

    public static bool IsCaptcha(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        if (HasCaptchaPathSignature(html))
            return true;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return IsCaptcha(doc);
    }

    public static bool IsCaptchaResponse(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return false;

        var trimmed = responseContent.TrimStart();
        return trimmed.StartsWith('<') && HasCaptchaPathSignature(trimmed);
    }

    private static bool HasCaptchaPathSignature(string content) =>
        content.Contains("/captcha_resp", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("/captcha.gif", StringComparison.OrdinalIgnoreCase);
}
