using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Legacy.ScheduleManager;

public interface ICourseScheduleService
{
    public Task<bool> TryReloadCourseDataAsync();
    public Course? GetCourse(string code);
    public CourseSection? GetCourseSection(string code, string group);
    public SectionChoice? GetSectionChoice(string code, string group);
}