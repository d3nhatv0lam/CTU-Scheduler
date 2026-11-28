using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Helpers;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

public class AcademicGradeClient : IAcademicGradeClient
{
    private readonly HttpClient _httpClient;
    private readonly IAcademicGradeHtmlParser _academicGradeHtmlParser;
    private readonly ILogger<AcademicGradeClient> _logger;

    public AcademicGradeClient(HttpClient client,IAcademicGradeHtmlParser academicGradeHtmlParser, ILogger<AcademicGradeClient> logger)
    {
        _httpClient = client;
        _academicGradeHtmlParser = academicGradeHtmlParser;
        _logger = logger;
    }

    public async Task<GradeTranscript> FetchTranscriptAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching transcript");

        using var request = new HttpRequestMessage(HttpMethod.Post,HtqlEndpoints.AcademicGrades);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "cmbKhoaHoc", "%" },
            { "cmbHocKy", "%" },
        });

        using var response = await _httpClient.SendAsync(request,cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new SessionExpiredException("Phiên đăng nhập đã hết hạn hoặc chưa đăng nhập.");
        }

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (CtuCaptchaDetector.IsCaptcha(html))
        {
            throw new CaptchaBlockedException("Hệ thống CTU yêu cầu xác thực Captcha.");
        }

        return _academicGradeHtmlParser.Parse(html, DateTimeOffset.UtcNow);
    }
}
