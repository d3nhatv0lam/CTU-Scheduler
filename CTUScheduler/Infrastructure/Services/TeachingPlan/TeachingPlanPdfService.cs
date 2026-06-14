using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public partial class TeachingPlanPdfService : ITeachingPlanPdfService
{
    private const string CacheSubFolder = "TeachingPlans";

    private readonly HttpClient _httpClient;
    private readonly ILogger<TeachingPlanPdfService> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

    public TeachingPlanPdfService(
        HttpClient httpClient,
        ILogger<TeachingPlanPdfService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult<string>> DownloadPdfAsync(
        string pdfUrl,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl))
        {
            return OperationResult<string>.Failed("Đường dẫn PDF tải về không hợp lệ.", kind: OperationFailureReason.System);
        }

        var filePath = GetCachedPdfPath(pdfUrl);
        var finalTimeout = timeout ?? _defaultTimeout;

        using var timeoutCts = new CancellationTokenSource(finalTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // --- CƠ CHẾ CACHE THÔNG MINH 2 TẦNG ---
        try
        {
            _logger.LogInformation("Checking remote PDF headers via HTTP HEAD for {Url}", pdfUrl);

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, pdfUrl);
            headRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            if (headResponse.IsSuccessStatusCode)
            {
                var serverLength = headResponse.Content.Headers.ContentLength;
                var serverLastModified = headResponse.Content.Headers.LastModified;

                if (File.Exists(filePath))
                {
                    var localInfo = new FileInfo(filePath);

                    // 1. Kiểm tra kích thước file khớp nhau
                    if (serverLength.HasValue && localInfo.Length == serverLength.Value)
                    {
                        // 2. Kiểm tra thời gian sửa đổi (nếu server có trả về Last-Modified)
                        if (!serverLastModified.HasValue || localInfo.LastWriteTimeUtc >= serverLastModified.Value.UtcDateTime)
                        {
                            _logger.LogInformation("PDF cache hit and validated via HTTP HEAD. Reusing cached file at {Path}", filePath);
                            return OperationResult<string>.Success(filePath);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("HTTP HEAD cache validation request timed out after {Timeout}s. Falling back to local cache.", finalTimeout.TotalSeconds);
            if (File.Exists(filePath) && new FileInfo(filePath).Length >= 1024)
            {
                return OperationResult<string>.Success(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform HTTP HEAD caching check on {Url}. Falling back to local basic cache validation.", pdfUrl);
            
            if (File.Exists(filePath))
            {
                var existingBytes = new FileInfo(filePath).Length;
                if (existingBytes >= 1024)
                {
                    _logger.LogInformation("Using cached PDF (fallback check) at {Path}", filePath);
                    return OperationResult<string>.Success(filePath);
                }
            }
        }

        // --- TIẾN HÀNH TẢI FILE NẾU CACHE CHƯA VALID/CHƯA TỒN TẠI ---
        try
        {
            _logger.LogInformation("Downloading PDF teaching plan from {Url}", pdfUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, pdfUrl);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, 81920, linkedCts.Token);

            _logger.LogInformation("PDF downloaded and cached at {Path}", filePath);
            return OperationResult<string>.Success(filePath);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<string>.Failed("Yêu cầu tải PDF đã bị hủy bởi người dùng.", kind: OperationFailureReason.System);
            }

            _logger.LogWarning("Download PDF request timed out after {Timeout}s", finalTimeout.TotalSeconds);
            return OperationResult<string>.Failed($"Yêu cầu tải file PDF bị quá thời gian chờ (Timeout {finalTimeout.TotalSeconds} giây).", kind: OperationFailureReason.Network);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error downloading PDF from {Url}", pdfUrl);
            return OperationResult<string>.Failed($"Lỗi kết nối mạng khi tải PDF: {ex.Message}", kind: OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System error downloading PDF from {Url}", pdfUrl);
            return OperationResult<string>.FromException(ex, "Không thể lưu hoặc ghi file PDF kế hoạch giảng dạy.");
        }
    }

    public string GetCachedPdfPath(string pdfUrl)
    {
        var cacheRoot = Path.Combine(AppConstants.Paths.BaseLocalPath, CacheSubFolder);
        Directory.CreateDirectory(cacheRoot);
        return Path.Combine(cacheRoot, ComputeSha256(pdfUrl) + ".pdf");
    }

    private static string ComputeSha256(string value)
    {
        int maxByteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> sourceBytes = maxByteCount <= 512 ? stackalloc byte[512] : new byte[maxByteCount];
        int written = Encoding.UTF8.GetBytes(value, sourceBytes);
        
        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(sourceBytes[..written], hashBytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
