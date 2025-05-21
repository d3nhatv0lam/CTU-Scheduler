using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable.Components
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
