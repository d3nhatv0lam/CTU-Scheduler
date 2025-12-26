using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

public class RuntimeCourseService : IRuntimeCourseService
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
    public void RegisterTimetable(ScheduleBlueprint blueprint)
    {
        var catalog = blueprint.Courses;
        var table = blueprint.Metadata;

        var catalogDict = catalog.ToDictionary(c => c.Code);
        _coursesSource.Edit(innerList =>
        {
            foreach (var (courseCode, groupCode) in table.SavedCourseGroupKeys)
            {
                var lookup = innerList.Lookup(courseCode);
                var runtimeCourse = lookup.HasValue
                    ? lookup.Value
                    : null;

                if (runtimeCourse is null)
                {
                    if (!catalogDict.TryGetValue(courseCode, out var courseDto))
                    {
                        continue; //(TKB lưu môn không có trong Catalog)
                    }

                    runtimeCourse = new RuntimeCourse(courseDto);
                    innerList.AddOrUpdate(runtimeCourse);
                }

                if (catalogDict.TryGetValue(courseCode, out var dto))
                {
                    var sectionToRegister = dto.Sections.FirstOrDefault(s => s.Group == groupCode);
                    if (sectionToRegister is not null)
                    {
                        runtimeCourse.RegisterSection(sectionToRegister);
                    }
                }
            }
        });
    }

    /// <summary>
    /// Hủy đăng ký TKB
    /// </summary>
    public void UnregisterTimetable(ScheduleProfile table)
    {
        _coursesSource.Edit(innerList =>
        {
            var keysToRemove = new List<string>();

            foreach (var (code, group) in table.SavedCourseGroupKeys)
            {
                var lookup = innerList.Lookup(code);
                if (!lookup.HasValue) continue;

                var runtimeCourse = lookup.Value;

                bool isEmpty = runtimeCourse.UnregisterSection(group);
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
    }

    public async Task RefreshCourseAsync()
    {
        var allCourses = _coursesSource.Items.ToList();
        _logger.LogInformation("Starting refresh for {Count} courses...", allCourses.Count);
        foreach (var runtimeCourse in allCourses)
        {
            try
            {
                var serverCourse = await _catalogService
                    .FetchCourseAsync(runtimeCourse.Code)
                    .ConfigureAwait(false);
                runtimeCourse.Merge(serverCourse);
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
}