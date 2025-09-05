using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Shared.Models;

namespace CTUScheduler.Presentation.Features.Scheduling.Models
{
    public class SelectableCourseData: SelectableItem<CourseSection>
    {
        public SelectableCourseData(CourseSection courseSection, bool isSelected = false): base(courseSection, isSelected)
        {

        }

        public static SelectableCourseData ToSelectableCourseData(CourseSection courseSection)
        {
            return new SelectableCourseData(courseSection);
        }
    }
}
