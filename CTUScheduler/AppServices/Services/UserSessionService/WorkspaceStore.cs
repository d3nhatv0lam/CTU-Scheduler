using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Helpers;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Utils.IO;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class WorkspaceStore : IWorkspaceStore
{
    private readonly IUserSessionService _userSessionService;
    private readonly IScheduleManager _scheduleManager;
    private readonly ILogger<WorkspaceStore> _logger;

    public WorkspaceStore(
        IUserSessionService userSessionService,
        IScheduleManager scheduleManager,
        ILogger<WorkspaceStore> logger)
    {
        _userSessionService = userSessionService;
        _scheduleManager = scheduleManager;
        _logger = logger;
    }

    public async Task<bool> SaveAsync(string filePath)
    {
        if (!PathUtils.IsValidFilePath(filePath, out var errorMessage))
        {
            _logger.LogError("Invalid save path: {ErrorMessage}", errorMessage);
            return false;
        }
        
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
        try
        {
            await JsonHelper.SerializeToSafeFileAsync(filePath, workspace);
            _userSessionService.NotifyModified();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("Access denied to write file. Check file permissions or Read-only attribute.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File is being used by another process.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown error during file save.");
        }
        return false;
    }

    public async Task<bool> LoadAsync(string filePath)
    {
        if (!PathUtils.IsValidFilePath(filePath, out var errorMessage))
        {
            _logger.LogError("Invalid save path: {ErrorMessage}", errorMessage);
            return false;
        }
        try
        {
            var option = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new SafeGuidConverter() }
            };
            var newWorkspace = await JsonHelper.DeserializeFromFileAsync<WorkspaceSnapshot>(filePath,option);
            if (newWorkspace is null)
            {
                _logger.LogError("Failed to load workspace from file {FilePath}.", filePath);
                return false;
            }

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
                _logger.LogWarning("Found broken references in Saved: {Warnings}", warnings);
            }

            _userSessionService.SetLocalContext(sanitizedWorkspace.Context);
            _userSessionService.SetLastModified(sanitizedWorkspace.LastModified);
            _scheduleManager.ImportSchedule(sanitizedWorkspace.Courses, sanitizedWorkspace.Profiles);
            return true;
        } catch (IOException ex)
        {
            _logger.LogError(ex, "File is being used by another process.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown error during load save.");
        }
        return false;
    }

    /// <summary>
    ///  Hỏng/mất tham chiếu giữa Profile - Course -> thêm vào warnings và xóa tham chiếu
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="warnings">Code:group brokenRef -> UI</param>
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