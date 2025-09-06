using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Academic;

namespace CTUScheduler.Presentation.Features.Scheduling.Models
{
    public class SelectableCourseSectionUi: SelectableItem<CourseSectionUi>
    {
        public SelectableCourseSectionUi(CourseSectionUi courseSection, bool isSelected = false): base(courseSection, isSelected)
        {

        }
    }
}
