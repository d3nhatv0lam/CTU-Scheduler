using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Helpers;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class WorkspaceStore : IWorkspaceStore
{
    private readonly IUserSessionService _userSessionService;
    private readonly IScheduleManager _scheduleManager;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ILogger<WorkspaceStore> _logger;

    public WorkspaceStore(
        IUserSessionService userSessionService,
        IScheduleManager scheduleManager,
        IWorkspaceRepository workspaceRepository,
        ILogger<WorkspaceStore> logger)
    {
        _userSessionService = userSessionService;
        _scheduleManager = scheduleManager;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task<OperationResult> SaveAsync(string filePath, CancellationToken ct = default)
    {
        var courseSnapshot = _scheduleManager.GetCoursesSnapshot().ToList();
        var profileSnapshot = _scheduleManager.GetProfileSnapshot().ToList();
        
        // Prune returns a new list
        var prunedCourses = ScheduleDataPruner.Prune(courseSnapshot, profileSnapshot);
        
        var workspace = new WorkspaceSnapshot(
            _userSessionService.CurrentContext,
            prunedCourses,
            profileSnapshot,
            DateTimeOffset.Now
        );

        var result = await _workspaceRepository.SaveAsync(workspace, filePath, ct);

        if (result.IsSuccess)
        {
            _userSessionService.NotifyModified();
        }

        return result;
    }

    public async Task<OperationResult> LoadAsync(string filePath, CancellationToken ct = default)
    {
        var loadResult = await _workspaceRepository.LoadAsync(filePath, ct).ConfigureAwait(false);

        if (loadResult.IsFailed)
        {
            return OperationResult.FailureFrom(loadResult);
        }

        var newWorkspace = loadResult.Content;

        // Đảm bảo Courses và Profiles không bao giờ null khi nạp từ file JSON cũ
        newWorkspace = newWorkspace with
        {
            Courses = newWorkspace.Courses ?? Array.Empty<Course>(),
            Profiles = newWorkspace.Profiles ?? Array.Empty<ScheduleProfile>()
        };

        // validate & sanitize
        var sanitizedWorkspace = SanitizeAndRepair(newWorkspace, out var warnings);
        
        if (warnings.Count > 0)
        {
            _logger.LogWarning("Found broken references in Saved: {Warnings}", string.Join(", ", warnings));
        }

        _userSessionService.SetLocalContext(sanitizedWorkspace.Context);
        _userSessionService.SetLastModified(sanitizedWorkspace.LastModified);
        _scheduleManager.ImportSchedule(sanitizedWorkspace.Courses, sanitizedWorkspace.Profiles);

        return OperationResult.Success();
    }

    /// <summary>
    ///  Hỏng/mất tham chiếu giữa Profile - Course -> thêm vào warnings và xóa tham chiếu
    /// </summary>
    private static WorkspaceSnapshot SanitizeAndRepair(WorkspaceSnapshot snapshot, out IReadOnlyList<string> warnings)
    {
        var currentWarnings = new List<string>();

        var availableKeys = snapshot.Courses
            .SelectMany(c => c.Sections.Select(s => (c.Code, s.Group)))
            .ToHashSet();

        var sanitizedProfiles = new List<ScheduleProfile>();

        foreach (var profile in snapshot.Profiles)
        {
            if (profile.SavedCourseGroupKeys is null || profile.SavedCourseGroupKeys.Count == 0)
                continue;

            // Kiểm tra các tham chiếu bị hỏng
            var brokenRefs = profile.SavedCourseGroupKeys
                .Where(kpv => !availableKeys.Contains((kpv.Key, kpv.Value)))
                .ToList();

            if (brokenRefs.Count > 0)
            {
                foreach (var (code, group) in brokenRefs)
                {
                    currentWarnings.Add($"{code}:{group}");
                    profile.SavedCourseGroupKeys.Remove(code);
                }
            }

            if (profile.SavedCourseGroupKeys.Count > 0)
            {
                sanitizedProfiles.Add(profile);
            }
        }

        warnings = currentWarnings;
        return snapshot with { Profiles = sanitizedProfiles };
    }
}