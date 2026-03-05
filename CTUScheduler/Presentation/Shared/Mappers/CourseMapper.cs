using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Presentation.Shared.Models.Academic;
using Riok.Mapperly.Abstractions;

namespace CTUScheduler.Presentation.Shared.Mappers;

[Mapper]
public partial class CourseMapper
{
    // Core -> UI
    public partial CourseUi ToCourseUi(Course course);
    public partial CourseSectionUi ToCourseSectionUi(CourseSection courseSection);
    public partial ClassDayUi ToClassDayUi(ClassDay classDay);
    
    // UI -> Core
    public partial Course ToCourse(CourseUi courseUi);
    public partial CourseSection ToCourseSection(CourseSectionUi courseSectionUi);
    public partial ClassDay ToClassDay(ClassDayUi classDayUi);
}