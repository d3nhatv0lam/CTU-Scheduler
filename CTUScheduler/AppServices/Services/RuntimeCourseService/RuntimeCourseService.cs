using System.Linq;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.RuntimeCourseService;

public class RuntimeCourseService: IRuntimeCourseService
{
    private readonly SourceCache<RuntimeCourse, string> _coursesSource;
    private readonly ICourseCatalogService _catalogService;

    public RuntimeCourseService(AppState appState)
    {
        _coursesSource = appState.RuntimeCoursesSource;
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

        foreach (var (courseCode, groupCode) in table.SavedCourseGroupKeys)
        {
            // kiểm tra có course này chưa
            var lookup = _coursesSource.Lookup(courseCode);
            RuntimeCourse runtimeCourse;

            if (lookup.HasValue)
            {
                runtimeCourse = lookup.Value;
            }
            else
            {
                if (!catalogDict.TryGetValue(courseCode, out var courseDto))
                {
                    // TKB lưu môn mà Catalog không có 
                    continue; 
                }
                runtimeCourse = new RuntimeCourse(courseDto);
                _coursesSource.AddOrUpdate(runtimeCourse);
            }
            // tìm course trong tham số catalog 
            if (catalogDict.TryGetValue(courseCode, out var dto))
            {
                var sectionToRegister = dto.Sections?.FirstOrDefault(s => s.Group == groupCode);
                if (sectionToRegister != null)
                {
                    runtimeCourse.RegisterSection(sectionToRegister);
                }
            }
        }
    }

    /// <summary>
    /// Hủy đăng ký TKB (Khi user xóa tab TKB hoặc tắt app)
    /// </summary>
    public void UnregisterTimetable(ScheduleProfile table)
    {
        foreach (var (code, group) in table.SavedCourseGroupKeys)
        {
            var lookup = _coursesSource.Lookup(code);
            if (!lookup.HasValue) continue;

            var runtimeCourse = lookup.Value;
            
            bool isEmpty = runtimeCourse.UnregisterSection(group);
            if (isEmpty)
            {
                runtimeCourse.Dispose(); 
                _coursesSource.Remove(code);
            }
        }
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