using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Contexts;
using HtmlAgilityPack;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

internal sealed class AuthClient : IAuthClient
{
    private readonly HttpClient _httpClient;

    public AuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginBootstrapContext> BootstrapAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(HtqlEndpoints.SessionKey, ct);
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

        using var checkResponse = await _httpClient.GetAsync(checkUrl, ct);
        if (!checkResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Kiểm tra sessionDataKey thất bại. Server trả về mã lỗi: {checkResponse.StatusCode}");
        }

        return new LoginBootstrapContext(sessionDataKey);
    }

    public async Task<SamlLoginContext> SubmitCredentialAsync(
        LoginBootstrapContext bootstrap,
        string username,
        string password,
        CancellationToken ct = default)
    {
        // bước 1: login vào cổng chính
        using var request = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.LoginSubmit);

        request.Headers.Add("Origin", "https://accounts.ctu.edu.vn");

        var formData = new Dictionary<string, string>
        {
            { "username", username },
            { "usernameUserInput", username },
            { "password", password },
            { "tocommonauth", "true" },
            { "sessionDataKey", bootstrap.SessionDataKey }
        };

        request.Content = new FormUrlEncodedContent(formData);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        // bước 2: gọi vào cổng SSO PHP của DKMH
        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.SsoAuth);

        var confirmData = new Dictionary<string, string>()
        {
            { "txtDinhDanh", username },
            { "txtMatKhau", string.Empty }
        };

        confirmRequest.Content = new FormUrlEncodedContent(confirmData);

        using var confirmResponse = await _httpClient.SendAsync(confirmRequest, ct);
        confirmResponse.EnsureSuccessStatusCode();

        await using var contentStream = await confirmResponse.Content.ReadAsStreamAsync(ct);

        var doc = new HtmlDocument();
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
    public async Task<bool> CompleteAuthenticationAsync(SamlLoginContext samlContext, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, HtqlEndpoints.SsoAuth);

        var formData = new Dictionary<string, string>
        {
            { "RelayState", "https://dkmh.ctu.edu.vn/htql/sinhvien/dang_nhap_sso.php" },
            { "SAMLResponse", samlContext.LoginSamlResponse },
        };

        request.Content = new FormUrlEncodedContent(formData);

        using var response = await _httpClient.SendAsync(request, ct);

        return response.IsSuccessStatusCode;
    }
}