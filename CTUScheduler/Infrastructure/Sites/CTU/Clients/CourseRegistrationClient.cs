using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Networking;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Clients;

/// <summary>
/// Client liên quan đến Đăng ký môn học 
/// </summary>
public class CourseRegistrationClient : IRegistrationRulesClient, ICourseRegistrationClient, ICourseCatalogClient
{
    private readonly HttpClient _httpClient;

    public CourseRegistrationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<RawQddkPayload> GetRegistrationInformationRawAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync(DkmhEndpoints.QuyDinh, null, ct);
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadCtuContentAsync<RawQddkPayload>(ct: ct);
        }
        catch (CtuDataContractException ex) when (ex.Message.Contains("mảng rỗng"))
        {
            throw new SessionExpiredException("Phiên đăng ký đã hết hạn hoặc chưa đăng nhập.", ex);
        }
    }

    public async Task<IReadOnlyList<QuickSelectDmhpCourse>> GetAutoCompleteQueryAsync(string keyword,
        CancellationToken ct = default)
    {
        var requestBody = new
        {
            data = new[]
            {
                new
                {
                    key = "dkmh_tu_dien_hoc_phan_ma_auto_complete",
                    type = "cmb",
                    parameters = new
                    {
                        dkmh_tu_dien_hoc_phan_ma = keyword,
                        limit = 20
                    }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(DkmhEndpoints.FilterHocPhan, requestBody, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new SessionExpiredException("Phiên đăng ký môn học đã hết hạn hoặc chưa đăng nhập.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadCtuContentAsync<List<QuickSelectDmhpCourse>>(
                element => element.TryGetProperty("data", out var data) &&
                           data.TryGetProperty("dkmh_tu_dien_hoc_phan_ma_auto_complete", out var auto)
                    ? auto
                    : default,
                ct: ct);
    }

    public async Task<RawDmhpPayload> GetCoursesRawAsync(int academicYear, int semester, string courseCode,
        CancellationToken ct = default)
    {
        var url = DkmhEndpoints.HocPhan.AbsoluteUri.ParseQueryString(new Dictionary<string, string>
        {
            { "dkmh_tu_dien_nien_khoa_nam_hoc", academicYear.ToString() },
            { "dkmh_tu_dien_hoc_ky_ma", semester.ToString() },
            { "dkmh_tu_dien_hoc_phan_ma", courseCode }
        });

        using var response = await _httpClient.GetAsync(url, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new SessionExpiredException("Phiên đăng ký môn học đã hết hạn hoặc chưa đăng nhập.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadCtuContentAsync<RawDmhpPayload>(ct: ct);
    }

    public async Task<List<RawDkhpPayload>> GetPlannedCoursesRawAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync(DkmhEndpoints.DaDangKy, null, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new SessionExpiredException("Phiên đăng ký môn học đã hết hạn hoặc chưa đăng nhập.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadCtuContentAsync<List<RawDkhpPayload>>(element =>
                element.TryGetProperty("data", out var data) &&
                data.TryGetProperty("data", out var value)
                    ? value
                    : default
            , ct: ct);
    }

    public async Task<RawThongTinHocPhiPayload> GetTuitionFeeRawAsync(int academicYear, int semester,
        CancellationToken ct = default)
    {
        var requestBody = new
        {
            dkmh_tu_dien_nien_khoa_nam_hoc = academicYear,
            dkmh_tu_dien_hoc_ky_ma = semester
        };

        using var response = await _httpClient.PostAsJsonAsync(DkmhEndpoints.HocPhi, requestBody, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new SessionExpiredException("Phiên đăng ký môn học đã hết hạn hoặc chưa đăng nhập.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadCtuContentAsync<RawThongTinHocPhiPayload>(ct: ct);
    }
}