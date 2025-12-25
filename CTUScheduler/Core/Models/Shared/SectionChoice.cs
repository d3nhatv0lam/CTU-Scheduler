using System;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

namespace CTUScheduler.Core.Models.Shared;

public record SectionChoice
{
    public Course Course { get; }
    public CourseSection Section { get; }
    public SectionChoice(Course course, CourseSection section)
    {
        Course = course ?? throw new ArgumentNullException(nameof(course));
        Section = section ?? throw new ArgumentNullException(nameof(section));

        if (course.Code != section.Code)
            throw new ArgumentException("Chosen section must belong to the given course.");
    }

    public void Deconstruct(out Course course, out CourseSection section)
    {
        course = this.Course;
        section = this.Section;
    }
}
