using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
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

    public Task<RawQddkPayload> GetRegistrationInformationRawAsync(CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<RawDkhpPayload> GetPlannedCoursesRawAsync(CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<RawThongTinHocPhiPayload> GetTuitionFeeRawAsync(CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<IReadOnlyList<QuickSelectDmhpCourse>> GetAutoCompleteQueryAsync(string keywork,
        CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<RawDmhpPayload> GetCourseRawAsync(string courseCode, CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }
}