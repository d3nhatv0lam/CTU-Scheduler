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
        
        var courseSnapshot = _scheduleManager.GetCourseSnapshot().ToList();
        var profileSnapshot = _scheduleManager.GetProfileSnapshot().ToList();
        ScheduleDataPruner.Trim(courseSnapshot, profileSnapshot);
        var workspace = new WorkspaceSnapshot()
        {
            Context = _userSessionService.CurrentContext ?? RegistrationContext.Unknown,
            Courses = courseSnapshot,
            Profiles = profileSnapshot,
            LastModified = DateTimeOffset.Now
        };
        try
        {
            await JsonHelper.SerializeToFileAsync(filePath, workspace);
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
                _logger.LogError("Failed to load workspace from file {FilePath}.}", filePath);
                return false;
            }
            // Sanitization
            newWorkspace.Courses ??= new List<Course>();
            newWorkspace.Profiles ??= new List<ScheduleProfile>();
            // validate
            SanitizeAndRepair(newWorkspace, out var warnings);
            if (warnings.Count > 0)
            {
                _logger.LogWarning("Found broken references in Saved: {Warnings}", warnings);
            }
            _userSessionService.SetContext(newWorkspace.Context);
            _userSessionService.SetLastModified(newWorkspace.LastModified);
            _scheduleManager.ImportSchedule(newWorkspace.Courses, newWorkspace.Profiles);
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
    private static void SanitizeAndRepair(WorkspaceSnapshot snapshot, out List<string> warnings)
    {
        warnings = new List<string>();

        var availableKeys = snapshot.Courses
            .SelectMany(c => c.Sections.Select(s => (c.Code, s.Group)))
            .ToHashSet();

        var profiles = snapshot.Profiles.ToList();
        foreach (var profile in profiles)
        {
            if (profile.SavedCourseGroupKeys is null || profile.SavedCourseGroupKeys.Count == 0)
            {
                snapshot.Profiles.Remove(profile);
                continue;
            }
            
            // xuất hiện trong profile mà không có trong course
            var brokenRef = profile.SavedCourseGroupKeys
                .Where(kpv => !availableKeys.Contains((kpv.Key, kpv.Value)))
                .ToList();

            if (brokenRef.Count > 0)
            {
                foreach (var (code, group) in brokenRef)
                {
                    warnings.Add($"{code}:{group}");
                    profile.SavedCourseGroupKeys.Remove(code);
                }
                if (profile.SavedCourseGroupKeys.Count == 0)
                    snapshot.Profiles.Remove(profile);
            }
        } 
    }
}