using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;
using CTUScheduler.Presentation.Shared.Controls.Timeline;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class TeachingPlanPdfService : ITeachingPlanPdfService
{
    private const string CacheSubFolder = "TeachingPlans";
    private static readonly Regex DateRangeRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})\s*[-–]\s*(\d{1,2}/\d{1,2}/\d{4})",
        RegexOptions.Compiled);
    private static readonly Regex SingleDateRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.Compiled);

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

            // Truyền linkedCts.Token vào request HEAD để tránh treo
            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            if (headResponse.IsSuccessStatusCode)
            {
                var serverLength = headResponse.Content.Headers.ContentLength;
                var serverLastModified = headResponse.Content.Headers.LastModified;

                if (File.Exists(filePath))
                {
                    var localInfo = new FileInfo(filePath);

                    // 1. Kiểm tra kích thước file khớp tuyệt đối
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

            // Truyền linkedCts.Token vào request GET tải file PDF
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, linkedCts.Token);

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

    private static string GetCachedPdfPath(string pdfUrl)
    {
        var cacheRoot = Path.Combine(AppConstants.Paths.BaseLocalPath, CacheSubFolder);
        Directory.CreateDirectory(cacheRoot);

        var hash = ComputeSha256(pdfUrl);
        var fileName = $"teaching-plan-{hash}.pdf";
        return Path.Combine(cacheRoot, fileName);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        var builder = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    public async Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new TeachingPlanData();
        }

        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                var text = string.Join(" ", document.GetPages().Select(p => p.Text ?? string.Empty));
                var data = new TeachingPlanData();

                var startTag = "NỘI DUNG CÔNG VIỆC THỜI GIAN THỰC HIỆN";
                var endTag = "Lưu ý:";
                
                var startIndex = text.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
                var endIndex = text.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

                if (startIndex == -1) return data;
                if (endIndex == -1) endIndex = text.Length;

                var tableContent = text.Substring(startIndex + startTag.Length, endIndex - (startIndex + startTag.Length)).Trim();
                
                var rowRegex = new Regex(@"(?<id>\d{1,2})\s+(?<content>.+?)\s+(?<date>\d{1,2}/\d{1,2}/\d{4}(?:\s*[-–]\s*\d{1,2}/\d{1,2}/\d{4})?)", RegexOptions.Singleline);
                
                var matches = rowRegex.Matches(tableContent);
                
                foreach (Match match in matches)
                {
                    var content = match.Groups["content"].Value;
                    var dateStr = match.Groups["date"].Value;

                    var rangeMatches = DateRangeRegex.Matches(dateStr);
                    if (rangeMatches.Count > 0)
                    {
                        if (TryParseDate(rangeMatches[0].Groups[1].Value, out var start) &&
                            TryParseDate(rangeMatches[0].Groups[2].Value, out var end))
                        {
                            data.RegistrationTimeline.Add(new TimelineNode(
                                NormalizeWhitespace(content),
                                start,
                                end));
                        }
                    }
                    else
                    {
                        var singleMatches = SingleDateRegex.Matches(dateStr);
                        if (singleMatches.Count > 0 && TryParseDate(singleMatches[0].Groups[1].Value, out var singleDate))
                        {
                            data.RegistrationTimeline.Add(new TimelineNode(
                                NormalizeWhitespace(content),
                                singleDate,
                                singleDate));
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PDF teaching plan at {Path}", filePath);
                return new TeachingPlanData();
            }
        });
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        if (DateOnly.TryParseExact(value, new[] { "d/M/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = dt;
            return true;
        }

        date = default;
        return false;
    }
}
