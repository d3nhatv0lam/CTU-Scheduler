using System;
using System.Text;

namespace CTUScheduler.Core.Utils;

public static class JwtRawDecoder
{
    public static string Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;

        var trimmedToken = token.Trim();
        var parts = trimmedToken.Split('.');

        if (parts.Length < 2) return string.Empty;

        var payload = parts[1];

        payload = payload.Replace('-', '+').Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
            case 1: throw new FormatException("Độ dài chuỗi payload JWT Base64Url không hợp lệ.");
        }

        try
        {
            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Không thể giải mã chuỗi Base64 của JWT payload.", ex);
        }
    }
}