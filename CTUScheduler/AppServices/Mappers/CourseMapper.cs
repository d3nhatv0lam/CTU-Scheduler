using System.Collections.Generic;
using System.Linq;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;
using Riok.Mapperly.Abstractions;

namespace CTUScheduler.AppServices.Mappers;

[Mapper]
public partial class CourseMapper
{
    public partial EditableCourse ToEditableCourse(Course course);

    private static SourceCache<CourseSection, string> MapSections(List<CourseSection> sections)
    {
        var target = new SourceCache<CourseSection, string>(section => section.Group);
        target.AddOrUpdate(sections);
        return target;
    }
    
    public partial Course ToCourse(EditableCourse editableCourse);
    
    private static List<CourseSection> MapSections(SourceCache<CourseSection, string> sections)
        => sections.Items.ToList();
}