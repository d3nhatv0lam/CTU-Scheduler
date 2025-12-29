using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

internal class RuntimeCourseService : IRuntimeCourseService
{
    private readonly SourceCache<RuntimeCourse, string> _coursesSource;
    private readonly ICourseCatalogService _catalogService;
    private readonly ILogger<RuntimeCourseService> _logger;

    public RuntimeCourseService(
        AppState appState,
        ICourseCatalogService catalogService,
        ILogger<RuntimeCourseService> logger)
    {
        _coursesSource = appState.RuntimeCoursesSource;
        
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký các môn học từ một Thời khóa biểu vào hệ thống Runtime
    /// </summary>
    /// <param name="blueprint"> Data lập thời khóa biểu</param>
    public bool RegisterTimetable(ScheduleBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var isRegistered = false;
        _coursesSource.Edit(innerList =>
        {
            isRegistered = ProcessSingleBlueprint(innerList, blueprint);
            if (!isRegistered)
            {
                _logger.LogWarning("RegisterTimetable failed. Blueprint inconsistent. Profile: {Metadata}", blueprint.Metadata);
            } 
        });
        return isRegistered;
    }

    /// <summary>
    /// Đăng ký nhiều tkb 1 lần
    /// </summary>
    public void RegisterTimetables(IEnumerable<ScheduleBlueprint> blueprints)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        _coursesSource.Edit(innerList =>
        {
            foreach (var blueprint in blueprints)
            {
                if (blueprint is null)
                {
                    _logger.LogWarning("Batch Register: Found a null blueprint in the list.");
                    continue;
                }
                if (!ProcessSingleBlueprint(innerList, blueprint))
                {
                    _logger.LogWarning("RegisterTimetable failed. Blueprint inconsistent. Profile: {Metadata}", blueprint.Metadata);
                }
            }
        });
    }
    
    /// <summary>
    /// Hủy đăng ký TKB
    /// </summary>
    public bool UnregisterTimetable(ScheduleProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        List<string> errorEntry = new();
        _coursesSource.Edit(innerList =>
        {
            // validate
            var (isValid, invalidEntries) = profile.ValidateRuntimeState(code => 
            {
                var lookup = innerList.Lookup(code);
                return lookup.HasValue ? lookup.Value : null;
            });
            
            if (!isValid)
            {
                errorEntry.AddRange(invalidEntries.Select(x => string.Concat(x.Code,"-",x.Group)));
                return;
            }
            
            // remove
            var keysToRemove = new List<string>();
            foreach (var (code, group) in profile.SavedCourseGroupKeys)
            {
                var lookup = innerList.Lookup(code);
                if (!lookup.HasValue) continue;
                
                var runtimeCourse = lookup.Value;

                bool isEmpty = runtimeCourse.UnregisterSection(group,profile.Id.ToString());
                if (isEmpty)
                {
                    runtimeCourse.Dispose();
                    keysToRemove.Add(code);
                }
            }

            if (keysToRemove.Count > 0)
            {
                innerList.RemoveKeys(keysToRemove);
            }
        });
        
        if (errorEntry.Count > 0)
        {
            _logger.LogWarning("UnregisterTimetable failed. Profile contains invalid entries: {InvalidEntries}", errorEntry);
            return false;
        }
        return true;
    }

    public async Task RefreshCourseAsync(CancellationToken token = default)
    {
        var allCourses = _coursesSource.Items.ToList();
        _logger.LogInformation("Starting refresh for {Count} courses...", allCourses.Count);
        foreach (var runtimeCourse in allCourses)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var serverCourse = await _catalogService
                    .FetchCourseAsync(runtimeCourse.Code, token)
                    .ConfigureAwait(false);
                runtimeCourse.Merge(serverCourse);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Refresh operation cancelled.");
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh course: {Code}", runtimeCourse.Code);
            }
        }
        _logger.LogInformation("Finished refreshing courses.");
    }
    public void ClearAll()
    {
        foreach (var course in _coursesSource.Items)
        {
            course.Dispose();
        }
        _coursesSource.Clear();
    }

    public IObservable<IChangeSet<RuntimeCourse, string>> Connect() => _coursesSource.Connect();

    /// <summary>
    /// Logic xử lý cốt lõi cho 1 blueprint bên trong transaction Edit
    /// </summary>
    private static bool ProcessSingleBlueprint(
        ISourceUpdater<RuntimeCourse, string> innerList, 
        ScheduleBlueprint blueprint)
    {
        Debug.Assert(innerList is not null);
        Debug.Assert(blueprint is not null);

        if (!blueprint.IsConsistent)
            return false;
        
        var catalogDict = blueprint.Courses.ToDictionary(c => c.Code);

        foreach (var (courseCode, groupCode) in blueprint.Metadata.SavedCourseGroupKeys)
        {
            // có trong Profile nhưng không có trong catalog -> mất ref
            if (!catalogDict.TryGetValue(courseCode, out var courseDto))
            {
                continue; 
            }
            
            var lookup = innerList.Lookup(courseCode);
            RuntimeCourse runtimeCourse;

            if (lookup.HasValue)
            {
                runtimeCourse = lookup.Value;
            }
            else
            {
                runtimeCourse = new RuntimeCourse(courseDto);
                innerList.AddOrUpdate(runtimeCourse);
            }
            
            var sectionToRegister = courseDto.Sections.FirstOrDefault(s => s.Group == groupCode);
            if (sectionToRegister is not null)
            {
                runtimeCourse.RegisterSection(sectionToRegister, blueprint.Metadata.Id.ToString());
            }
        }
        return true;
    }
}