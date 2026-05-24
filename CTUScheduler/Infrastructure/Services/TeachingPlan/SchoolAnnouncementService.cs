using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Models;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class SchoolAnnouncementService : ISchoolAnnouncementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SchoolAnnouncementService> _logger;
    private const string ApiUrl = "https://htql.ctu.edu.vn/htql/thongbao.php";
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

    public SchoolAnnouncementService(HttpClient httpClient, ILogger<SchoolAnnouncementService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<SchoolAnnouncement>>> FetchAnnouncementsAsync(
        CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? _defaultTimeout;

        using var timeoutCts = new CancellationTokenSource(finalTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);

            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Referer", "https://accounts.ctu.edu.vn/");
            request.Headers.Add("Origin", "https://accounts.ctu.edu.vn");
            request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");

            using var response = await _httpClient.SendAsync(request, linkedCts.Token);

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<List<SchoolAnnouncement>>(cancellationToken);

            return data ?? [];
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed("User cancelled the operation");
            }

            _logger.LogWarning("Fetch announcements timed out after {Timeout}s", finalTimeout.TotalSeconds);
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed(
                $"Request timed out after {finalTimeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch announcements");
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.FromException(ex);
        }
    }
}