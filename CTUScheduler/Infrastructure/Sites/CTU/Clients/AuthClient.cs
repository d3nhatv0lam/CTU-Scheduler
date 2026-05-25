using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Contexts;
using HtmlAgilityPack;
using ReactiveUI;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

internal sealed class AuthClient : IAuthClient
{
    public async Task<CtuSession> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var cookiesContainer = new CookieContainer();
        using var httpHandler = new HttpClientHandler();
        httpHandler.CookieContainer = cookiesContainer;
        using var httpClient = new HttpClient(httpHandler);

        var bootstrapContext = await BootstrapAsync(httpClient, ct);
        var samlContext = await SubmitCredentialAsync(httpClient, bootstrapContext, username, password, ct);
        var isAuthenticated = await CompleteAuthenticationAsync(httpClient, samlContext, ct);

        if (!isAuthenticated)
        {
            // không thiết lập được Cookie
            throw new SessionExpiredException("Không thể hoàn tất thiết lập phiên Cookie");
        }

        var dkmhJwtToken = await GetDkmhJwtTokenAsync(httpClient, cookiesContainer, username, ct);
        
        var legacyCookies = new Dictionary<string, string>();
        DateTimeOffset? cookiesExpiresAt = null;
        foreach (Cookie cookie in cookiesContainer.GetCookies(DkmhEndpoints.BaseDomain))
        {
            if (cookie.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
                continue;
            
            legacyCookies.Add(cookie.Name, cookie.Value);

            if (cookie.Name.Equals("SESSISID", StringComparison.OrdinalIgnoreCase) &&
                cookie.Expires != DateTime.MinValue)
            {
                cookiesExpiresAt = cookie.Expires;
            }
        }
        
        // dịch jwt ra lấy ngày hết hạn, userinfo rồi trả về CtuSession
        

        throw new NotImplementedException();
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

        // bước 2: gọi vào cổng SSO PHP của DKMH
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

        var inputNode =
            doc.DocumentNode.SelectSingleNode("//form[@id='samlsso-response-form']//input[@name='SAMLResponse']");

        if (inputNode is null)
            throw new InvalidOperationException(
                "Không tìm thấy thuộc tính 'SAMLResponse' trong trang.");

        var samlResponse = inputNode.GetAttributeValue("value", string.Empty);

        if (string.IsNullOrEmpty(samlResponse))
            throw new InvalidOperationException(nameof(samlResponse) + " bị rỗng");

        return new SamlLoginContext(samlResponse);
    }

    /// <summary>
    /// Gửi SAML Response để kích hoạt và thiết lập phiên Cookie chính thức trên phân hệ DKMH Web.
    /// </summary>
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

    /// <summary>
    /// Dùng cookies của CTU để xin jwt
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="ct"></param>
    /// <param name="cookieContainer"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    private static async Task<string> GetDkmhJwtTokenAsync(HttpClient httpClient,
        CookieContainer cookieContainer,
        string username,
        CancellationToken ct = default)
    {
        // Gọi lấy relayState và samlResponse
        // default của body
        var password = "p";
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, DkmhEndpoints.Login);
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "txtTaiKhoan", username },
            { "txtMatKhau", password }
        });

        using var loginResponse = await httpClient.SendAsync(loginRequest, ct);
        loginResponse.EnsureSuccessStatusCode();

        await using var loginResponseStream = await loginResponse.Content.ReadAsStreamAsync(ct);

        var doc = new HtmlDocument();
        doc.Load(loginResponseStream);

        var relayStateNode =
            doc.DocumentNode.SelectSingleNode("//*[@id='samlsso-response-form']//input[@name='RelayState']");
        if (relayStateNode is null)
        {
            throw new InvalidOperationException("Không tìm thấy thuộc tính 'RelayState' trong trang phản hồi SSO.");
        }

        string relayState = relayStateNode.GetAttributeValue("value", string.Empty);

        var samlResponseNode =
            doc.DocumentNode.SelectSingleNode("//*[@id='samlsso-response-form']//input[@name='SAMLResponse']");
        if (samlResponseNode is null)
        {
            throw new InvalidOperationException("Không tìm thấy thuộc tính 'SAMLResponse' trong trang phản hồi SSO.");
        }

        string samlResponse = samlResponseNode.GetAttributeValue("value", string.Empty);


        if (string.IsNullOrEmpty(relayState) || string.IsNullOrEmpty(samlResponse))
        {
            throw new InvalidOperationException("Dữ liệu RelayState hoặc SAMLResponse trích xuất bị rỗng.");
        }

        // xin jwt
        var jwtRequest = new HttpRequestMessage(HttpMethod.Post, DkmhEndpoints.GetToken);
        jwtRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "RelayState", relayState },
            { "SAMLResponse", samlResponse }
        });
        using var jwtResponse = await httpClient.SendAsync(jwtRequest, ct);
        jwtResponse.EnsureSuccessStatusCode();

        var jwtToken = cookieContainer.GetCookies(HtqlEndpoints.BaseDomain)
            .FirstOrDefault(c => c.Name == "access_token")
            ?.Value;

        if (string.IsNullOrEmpty(jwtToken))
        {
            throw new InvalidOperationException(
                "Không tìm thấy Cookie 'access_token' trong CookieContainer sau khi Redirect.");
        }

        return jwtToken;
    }
}