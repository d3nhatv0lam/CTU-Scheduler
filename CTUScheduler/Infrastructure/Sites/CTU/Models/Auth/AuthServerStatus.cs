namespace CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;

public enum AuthServerStatus
{
    Healthy,          // Máy chủ bình thường, sẵn sàng đăng nhập
    CaptchaBlocked,   // IP bị WAF chặn và yêu cầu giải Captcha
    Maintenance,      // Máy chủ trường đang bảo trì hoặc trả về lỗi 5xx
    Unreachable       // Không thể kết nối (mất mạng, DNS lỗi, timeout)
}