using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static class CtuHttpExtensions
{
    /// <summary>
    /// Gửi POST JSON đã buffer sẵn byte để luôn có Content-Length và Content-Type: application/json thuần,
    /// tránh bị WAF của CTU từ chối Transfer-Encoding: chunked / charset=utf-8 từ PostAsJsonAsync.
    /// </summary>
    public static async Task<HttpResponseMessage> PostBufferedJsonAsync<TBody>(
        this HttpClient httpClient,
        Uri requestUri,
        TBody body,
        CancellationToken ct = default)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new ByteArrayContent(jsonBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    public static async Task<T> ReadCtuContentAsync<T>(
        this HttpContent content,
        Func<JsonElement, JsonElement>? elementSelector = null,
        CancellationToken ct = default)
    {
        // Đọc trực tiếp luồng nhị phân (Stream) từ mạng để nạp vào JsonDocument
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = document.RootElement;

        // Đọc nhanh mã lỗi nghiệp vụ tập trung
        int code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetInt32() : 500;
        string msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() ?? "Unknown Error" : "Unknown Error";

        if (code != 200)
        {
            if (code == 4012)
            {
                throw new SessionExpiredException(msg);
            }
            throw new CtuApiException($"Lỗi từ hệ thống CTU: {msg} (Mã lỗi: {code})");
        }

        // Trích xuất nút dữ liệu đích cần giải tuần tự
        JsonElement targetElement;
        if (elementSelector != null)
        {
            targetElement = elementSelector(root);
        }
        else
        {
            // default data của CTU
            if (!root.TryGetProperty("data", out targetElement))
            {
                throw new CtuDataContractException("Cấu trúc phản hồi từ máy chủ CTU thiếu trường dữ liệu 'data'.");
            }
        }

        // Kiểm tra Undefined (khi selector không tìm thấy trường)
        if (targetElement.ValueKind == JsonValueKind.Undefined)
        {
            throw new CtuDataContractException("Cấu trúc dữ liệu phản hồi từ máy chủ CTU đã thay đổi hoặc không hợp lệ.");
        }

        // Giải quyết lỗi "máy chủ trả về [] thay vì object/null" của CTU
        // Kiểm tra xem targetElement có phải là mảng JSON (Array) trong khi T không phải dạng danh sách
        if (targetElement.ValueKind == JsonValueKind.Array &&
            !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)) &&
            typeof(T) != typeof(string))
        {
            throw new CtuDataContractException("Cấu trúc phản hồi từ máy chủ CTU nhận được mảng rỗng thay vì đối tượng dữ liệu.");
        }

        // Giải tuần tự hóa trực tiếp và bảo vệ tránh lỗi/null
        try
        {
            var result = JsonSerializer.Deserialize<T>(targetElement.GetRawText());
            if (result is null)
            {
                throw new CtuDataContractException("Dữ liệu phản hồi từ máy chủ CTU trống hoặc không thể giải tuần tự hóa.");
            }
            return result;
        }
        catch (JsonException ex)
        {
            throw new CtuDataContractException("Cấu trúc dữ liệu phản hồi từ máy chủ CTU không khớp với mô hình dữ liệu trong ứng dụng.", ex);
        }
    }
}
