using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Presentation.Features.Scheduling.Models
{
    public class SelectableCourseData: SelectableItem<CourseData>
    {
        public SelectableCourseData(CourseData courseData, bool isSelected = false): base(courseData, isSelected)
        {

        }

        public static SelectableCourseData ToSelectableCourseData(CourseData courseData)
        {
            return new SelectableCourseData(courseData);
        }
    }
}
