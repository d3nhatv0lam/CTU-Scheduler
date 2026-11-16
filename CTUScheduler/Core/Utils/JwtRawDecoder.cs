using System;
using System.Buffers.Text;
using System.Text;

namespace CTUScheduler.Core.Utils;

public static class JwtRawDecoder
{
    public static string Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;

        // Định vị ranh giới của Payload nằm giữa 2 dấu chấm
        int firstDot = token.IndexOf('.');
        int secondDot = token.IndexOf('.', firstDot + 1);

        if (firstDot < 0 || secondDot <= firstDot) return string.Empty;

        // Cắt lát bộ nhớ trực tiếp không cấp phát chuỗi mới (Zero-allocation)
        var payloadSpan = token.AsSpan(firstDot + 1, secondDot - firstDot - 1);

        try
        {
            var bytes = Base64Url.DecodeFromChars(payloadSpan);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Chuỗi Base64Url của JWT payload không hợp lệ.", ex);
        }
    }
}