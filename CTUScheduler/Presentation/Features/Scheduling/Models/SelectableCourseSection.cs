using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Presentation.Shared.Models;

namespace CTUScheduler.Presentation.Features.Scheduling.Models
{
    public class SelectableCourseSection: SelectableItem<CourseSection>
    {
        public SelectableCourseSection(CourseSection courseSection, bool isSelected = false): base(courseSection, isSelected)
        {

        }
    }
}
