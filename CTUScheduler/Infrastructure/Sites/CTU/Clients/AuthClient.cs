using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Networking;
using CTUScheduler.Core.Utils;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

internal sealed record LoginBootstrapContext(string SessionDataKey);

internal sealed record SamlLoginContext(string LoginSamlResponse);

internal sealed class AuthClient : IAuthClient
{
    private readonly HttpClient _pingHttpClient;
    private readonly ILogger<AuthClient> _logger;

    public AuthClient(HttpClient pingHttpClient, ILogger<AuthClient> logger)
    {
        _pingHttpClient = pingHttpClient;
        _logger = logger;
    }

    public async Task<CtuSession> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        _logger.LogInformation("Bắt đầu tiến trình xác thực SSO CTU cho tài khoản {Username}...", username);
        var cookiesContainer = new CookieContainer();
        using var httpHandler = new HttpClientHandler();
        httpHandler.CookieContainer = cookiesContainer;
        using var httpClient = new HttpClient(httpHandler);

        _logger.LogDebug("Khởi tạo session key (Bootstrap)...");
        var bootstrapContext = await BootstrapAsync(httpClient, ct);
        
        _logger.LogDebug("Gửi thông tin đăng nhập xác thực SSO...");
        var samlContext = await SubmitCredentialAsync(httpClient, bootstrapContext, username, password, ct);
        
        _logger.LogDebug("Hoàn tất thiết lập phiên trên Web HTQL...");
        var isAuthenticated = await CompleteAuthenticationAsync(httpClient, samlContext, ct);

        if (!isAuthenticated)
        {
            throw new InvalidOperationException("Không thể hoàn tất thiết lập phiên Cookie trên phân hệ Web.");
        }

        _logger.LogDebug("Thực hiện bắt tay SSO lấy Token API JWT...");
        var jwtSsoSuccess = await ExecuteDkmhJwtHandshakeAsync(httpClient, cookiesContainer, username, ct);
        if (!jwtSsoSuccess)
        {
            throw new InvalidOperationException(
                "Bắt tay xác thực SSO API JWT thất bại hoặc máy chủ DKMHBack từ chối kết nối.");
        }

        _logger.LogInformation("Xác thực và bắt tay SSO thành công! Bắt đầu trích xuất cookies và token...");

        var targetDomains = new[]
        {
            DkmhEndpoints.BaseDomain,
            HtqlEndpoints.AccountsDomain,
            HtqlEndpoints.BaseDomain
        };

        // Trích xuất access_token (JWT)
        string dkmhJwtToken = targetDomains
            .SelectMany(domain => cookiesContainer.GetCookies(domain))
            .FirstOrDefault(c => c.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(dkmhJwtToken))
        {
            throw new InvalidOperationException(
                "Không tìm thấy Cookie 'access_token' trong CookieContainer sau khi đăng nhập.");
        }

        // Trích xuất các legacy cookies và thời gian hết hạn phiên
        var (legacyCookies, cookiesExpiresAt) = ExtractLegacyCookies(cookiesContainer, targetDomains);

        var (studentProfile, dkmhJwtExpiresAt) = ParseJwt(dkmhJwtToken);

        return new CtuSession(
            Dkmh: new DkmhSession(dkmhJwtToken, dkmhJwtExpiresAt, studentProfile),
            Htql: new HtqlSession(legacyCookies, cookiesExpiresAt)
        );
    }

    public async Task<bool> PingSessionAsync(CancellationToken ct = default)
    {
        using var response = await _pingHttpClient.GetAsync(HtqlEndpoints.StudentHome, ct);

        if (!response.IsSuccessStatusCode)
            return false;

        var htmlContent = await response.Content.ReadAsStringAsync(ct);
        return !htmlContent.Contains("location.href='../logout.php'");
    }

    public async Task<CtuSession?> TrySilentReAuthAsync(CtuSession currentSession, CancellationToken ct = default)
    {
        try
        {
            var cookiesContainer = new CookieContainer();
            var targetDomains = new[]
            {
                HtqlEndpoints.BaseDomain,
                HtqlEndpoints.AccountsDomain,
                DkmhEndpoints.BaseDomain,
                HtqlEndpoints.HtqlDomain
            };

            var sourceCookies = currentSession.Htql?.Cookies ?? new Dictionary<string, string>();

            foreach (var cookie in sourceCookies)
            {
                // truyền cookie vào domain .ctu...
                try
                {
                    cookiesContainer.Add(new Cookie(cookie.Key, cookie.Value)
                    {
                        Domain = ".ctu.edu.vn"
                    });
                }
                catch
                {
                    /* ignore */
                }

                // force cookie vào từng domain
                foreach (var uri in targetDomains)
                {
                    try
                    {
                        cookiesContainer.Add(uri, new Cookie(cookie.Key, cookie.Value));
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            }

            using var httpHandler = new HttpClientHandler();
            httpHandler.CookieContainer = cookiesContainer;
            using var httpClient = new HttpClient(httpHandler);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

            // bắt tay xin lại PHPSESSID
            bool isSuccess = await ExecuteLegacySsoHandshakeAsync(httpClient, currentSession.StudentId, ct);
            if (!isSuccess) return null;

            // Trích xuất và cập nhật các legacy cookies mới nhận được sau khi re-auth
            var (newCookies, cookiesExpiresAt) = ExtractLegacyCookies(
                cookiesContainer,
                [HtqlEndpoints.StudentHome, HtqlEndpoints.AccountsDomain],
                sourceCookies);

            var refreshedHtql = new HtqlSession(newCookies, cookiesExpiresAt);

            // thử gia hạn JWT để kéo dài thêm 12 tiếng liên tục
            DkmhSession? refreshedDkmh = null;
            try
            {
                bool jwtSuccess = await ExecuteDkmhJwtHandshakeAsync(httpClient, cookiesContainer, currentSession.StudentId, ct);
                if (jwtSuccess)
                {
                    var dkmhCookies = cookiesContainer.GetCookies(HtqlEndpoints.BaseDomain);
                    string? newJwtToken = dkmhCookies
                        .FirstOrDefault(c => c.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
                        ?.Value;

                    if (!string.IsNullOrEmpty(newJwtToken))
                    {
                        var (studentProfile, dkmhJwtExpiresAt) = ParseJwt(newJwtToken);
                        refreshedDkmh = new DkmhSession(newJwtToken, dkmhJwtExpiresAt, studentProfile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gia hạn JWT ngầm thất bại (sẽ fallback giữ JWT cũ).");
            }

            return currentSession with
            {
                Htql = refreshedHtql,
                Dkmh = refreshedDkmh ?? currentSession.Dkmh
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi cố gắng khôi phục phiên ngầm HtqlSession (TrySilentReAuthAsync).");
            return null;
        }
    }
    /// <summary>
    /// Thực hiện bắt tay SSO Legacy PHP (Gửi định danh rỗng mật khẩu -> Lấy SAML -> POST SAML -> Hoàn tất)
    /// </summary>
    private static async Task<bool> ExecuteLegacySsoHandshakeAsync(HttpClient httpClient, string studentId,
        CancellationToken ct)
    {
        // Bước 1: Khởi động SSO chuyển hướng lấy SAML
        using var ssoRequest = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.SsoAuth);
        ssoRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "txtDinhDanh", studentId },
            { "txtMatKhau", string.Empty }
        });

        using var response = await httpClient.SendAsync(ssoRequest, ct);
        if (!response.IsSuccessStatusCode) return false;

        var htmlText = await response.Content.ReadAsStringAsync(ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlText);

        string samlResponse;
        try
        {
            samlResponse = ExtractSamlResponse(doc);
        }
        catch
        {
            return false;
        }

        // Gửi SAML Response để kích hoạt và thiết lập phiên Cookie PHPSESSID
        var samlContext = new SamlLoginContext(samlResponse);
        return await CompleteAuthenticationAsync(httpClient, samlContext, ct);
    }

    /// <summary>
    /// Thực hiện bắt tay SSO API để nhận cookie access_token (JWT) ngầm
    /// </summary>
    private static async Task<bool> ExecuteDkmhJwtHandshakeAsync(
        HttpClient httpClient,
        CookieContainer cookieContainer,
        string username,
        CancellationToken ct)
    {
        // Bước 1: Gửi yêu cầu đăng nhập lấy SAML API
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, DkmhEndpoints.Login);
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "txtTaiKhoan", username },
            { "txtMatKhau", "p" }
        });

        using var loginResponse = await httpClient.SendAsync(loginRequest, ct);
        if (!loginResponse.IsSuccessStatusCode) return false;

        var doc = new HtmlDocument();
        using var loginResponseStream = await loginResponse.Content.ReadAsStreamAsync(ct);
        doc.Load(loginResponseStream);

        var relayStateNode =
            doc.DocumentNode.SelectSingleNode("//*[@id='samlsso-response-form']//input[@name='RelayState']");
        var samlResponseNode =
            doc.DocumentNode.SelectSingleNode("//*[@id='samlsso-response-form']//input[@name='SAMLResponse']");

        if (relayStateNode is null || samlResponseNode is null)
        {
            return false;
        }

        string relayState = relayStateNode.GetAttributeValue("value", string.Empty);
        string samlResponse = samlResponseNode.GetAttributeValue("value", string.Empty);

        if (string.IsNullOrEmpty(relayState) || string.IsNullOrEmpty(samlResponse))
        {
            return false;
        }

        // Bước 2: Đệ trình SAML để lấy token JWT
        var jwtRequest = new HttpRequestMessage(HttpMethod.Post, DkmhEndpoints.GetToken);
        jwtRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "RelayState", relayState },
            { "SAMLResponse", samlResponse }
        });
        using var jwtResponse = await httpClient.SendAsync(jwtRequest, ct);
        if (!jwtResponse.IsSuccessStatusCode) return false;

        // Xác thực sự hiện diện của Cookie JWT trong container
        var jwtToken = cookieContainer.GetCookies(HtqlEndpoints.BaseDomain)
            .FirstOrDefault(c => c.Name == "access_token")
            ?.Value;

        return !string.IsNullOrEmpty(jwtToken);
    }

    private static async Task<LoginBootstrapContext> BootstrapAsync(HttpClient httpClient,
        CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync(HtqlEndpoints.SessionKey, ct);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);

        var doc = new HtmlDocument();
        doc.Load(contentStream);

        var inputNode = doc.DocumentNode.SelectSingleNode("//form[@id='loginForm']//input[@name='sessionDataKey']");

        if (inputNode is null)
            throw new InvalidOperationException(
                "Không tìm thấy thuộc tính 'sessionDataKey' trong trang Đăng nhập của trường.");

        var sessionDataKey = inputNode.GetAttributeValue("value", string.Empty);

        if (string.IsNullOrEmpty(sessionDataKey))
            throw new InvalidOperationException(nameof(sessionDataKey) + " bị rỗng");

        string checkUrl =
            $"{HtqlEndpoints.PrefetchKey}?sessionDataKey={Uri.EscapeDataString(sessionDataKey)}&application=CTU_SP&authenticators=BasicAuthenticator%3ALOCAL";

        using var checkResponse = await httpClient.GetAsync(checkUrl, ct);
        if (!checkResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Kiểm tra sessionDataKey thất bại. Server trả về mã lỗi: {checkResponse.StatusCode}");
        }

        return new LoginBootstrapContext(sessionDataKey);
    }

    private static async Task<SamlLoginContext> SubmitCredentialAsync(
        HttpClient httpClient,
        LoginBootstrapContext bootstrap,
        string username,
        string password,
        CancellationToken ct = default)
    {
        // bước 1: login vào cổng chính
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.LoginSubmit);

        loginRequest.Headers.Add("Origin", "https://accounts.ctu.edu.vn");
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", username },
            { "usernameUserInput", username },
            { "password", password },
            { "tocommonauth", "true" },
            { "sessionDataKey", bootstrap.SessionDataKey }
        });

        using var response = await httpClient.SendAsync(loginRequest, ct);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);

        var doc = new HtmlDocument();
        doc.Load(responseStream);

        var errNode = doc.DocumentNode.SelectSingleNode("//div[@id='error-msg']");
        if (errNode is not null)
        {
            throw new InvalidCredentialsException(errNode.InnerText?.Trim() ??
                                                  "Mã số đăng nhập hoặc Mật khẩu không đúng.");
        }

        // gọi vào cổng SSO PHP của DKMH
        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.SsoAuth);

        confirmRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "txtDinhDanh", username },
            { "txtMatKhau", string.Empty }
        });

        using var confirmResponse = await httpClient.SendAsync(confirmRequest, ct);
        confirmResponse.EnsureSuccessStatusCode();

        await using var contentStream = await confirmResponse.Content.ReadAsStreamAsync(ct);
        doc.Load(contentStream);

        var samlResponse = ExtractSamlResponse(doc);

        return new SamlLoginContext(samlResponse);
    }

    private static async Task<bool> CompleteAuthenticationAsync(HttpClient httpClient, SamlLoginContext samlContext,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.SsoAuth);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "RelayState", "https://dkmh.ctu.edu.vn/htql/sinhvien/dang_nhap_sso.php" },
            { "SAMLResponse", samlContext.LoginSamlResponse },
        });

        using var response = await httpClient.SendAsync(request, ct);

        return response.IsSuccessStatusCode;
    }

    private static (StudentProfile Profile, DateTimeOffset ExpiresAt) ParseJwt(string jwtToken)
    {
        var jwtDecoded = JwtRawDecoder.Decode(jwtToken);

        if (string.IsNullOrEmpty(jwtDecoded))
            throw new InvalidOperationException("dkmh jwt token không hợp lệ");

        using var jwtDoc = JsonDocument.Parse(jwtDecoded);

        if (!jwtDoc.RootElement.TryGetProperty("exp", out var expProp) || expProp.ValueKind != JsonValueKind.Number)
            throw new InvalidOperationException("exp không hợp lệ trong JWT");

        var expSeconds = expProp.GetInt64();
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);

        if (!jwtDoc.RootElement.TryGetProperty("user_info", out var userInfoProp))
            throw new InvalidOperationException("Không tìm thấy trường user_info trong JWT");

        var userInfoZlib = userInfoProp.GetString();
        if (string.IsNullOrEmpty(userInfoZlib))
            throw new InvalidOperationException("user_info bị rỗng");

        var decompressedJson = ZlibHelper.DecompressZlib(userInfoZlib);

        using var profileDoc = JsonDocument.Parse(decompressedJson);
        var root = profileDoc.RootElement;

        var profile = new StudentProfile(
            Mssv: GetStr("sys_manguoidung"),
            Name: GetStr("sys_hoten"),
            ClassCode: GetStr("sys_malop"),
            MajorName: GetStr("sys_tennganh"),
            DepartmentName: GetStr("sys_tendonvi"),
            Cohort: GetInt("sys_khoahoc"),
            AccumulatedCredits: GetInt("sys_sotinchidat"),
            CurrentAcademicYear: GetInt("sys_namhocht"),
            CurrentSemester: GetInt("sys_hockyht"),
            MaxCreditsMainSemester: GetInt("sys_tcmaxhockychinh"),
            MaxCreditsSummerSemester: GetInt("sys_tcmaxhockyhe")
        );

        return (profile, expiresAt);

        string GetStr(string propName) =>
            root.TryGetProperty(propName, out var prop) ? prop.GetString() ?? "" : "";

        int GetInt(string propName) =>
            root.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : 0;
    }

    private static string ExtractSamlResponse(HtmlDocument doc)
    {
        var samlNode =
            doc.DocumentNode.SelectSingleNode("//form[@id='samlsso-response-form']//input[@name='SAMLResponse']");

        if (samlNode is null)
            throw new InvalidOperationException("Không tìm thấy thuộc tính 'SAMLResponse' trong trang phản hồi SSO.");

        var samlResponse = samlNode.GetAttributeValue("value", string.Empty);

        if (string.IsNullOrEmpty(samlResponse))
            throw new InvalidOperationException(nameof(samlResponse) + " bị rỗng");

        return samlResponse;
    }

    private static (Dictionary<string, string> Cookies, DateTimeOffset ExpiresAt) ExtractLegacyCookies(
        CookieContainer cookieContainer,
        IEnumerable<Uri> domains,
        IDictionary<string, string>? initialCookies = null)
    {
        var cookies = initialCookies is not null
            ? new Dictionary<string, string>(initialCookies)
            : new Dictionary<string, string>();

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(12);

        foreach (var domain in domains)
        {
            var cookieCollection = cookieContainer.GetCookies(domain);
            foreach (Cookie cookie in cookieCollection)
            {
                if (cookie.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                cookies[cookie.Name] = cookie.Value;

                if (cookie.Name.Equals("SESSISID", StringComparison.OrdinalIgnoreCase) &&
                    cookie.Expires != DateTime.MinValue)
                {
                    expiresAt = cookie.Expires;
                }
            }
        }

        return (cookies, expiresAt);
    }
}