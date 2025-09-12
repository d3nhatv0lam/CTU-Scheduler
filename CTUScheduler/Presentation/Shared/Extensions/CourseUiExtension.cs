using System.Collections.Generic;
using System.Collections.ObjectModel;
using CTUScheduler.Presentation.Shared.Models.Academic;

namespace CTUScheduler.Presentation.Shared.Extensions;

public static class CourseUiExtension
{
    public static CourseUi CloneWithNewCourseSections(this CourseUi course, IEnumerable<CourseSectionUi> sections)
    {
        return new CourseUi
        {
            Code = course.Code,
            Name_VN = course.Name_VN,
            Credit = course.Credit,
            TheorySessions = course.TheorySessions,
            PracticalSessions = course.PracticalSessions,
            Sections = new ObservableCollection<CourseSectionUi>(sections)
        };
    }
}