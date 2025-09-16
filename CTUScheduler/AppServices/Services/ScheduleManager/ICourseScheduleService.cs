using System;
using System.Reactive;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public interface ICourseScheduleService
{
    public Task ReloadCourseDataAsync();
    public Course? GetCourse(string code);
    public CourseSection? GetCourseSection(string code, string group);
    public SectionChoice? GetSectionChoice(string code, string group);
}