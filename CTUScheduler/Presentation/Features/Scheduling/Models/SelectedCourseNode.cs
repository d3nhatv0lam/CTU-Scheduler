using System.Collections.Generic;
using System.Collections.ObjectModel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SelectedCourseNode: ReactiveObject
{
    public Course CoreCourse { get; }
    public ObservableCollection<CourseSection> Sections { get; }

    public SelectedCourseNode(Course course, IEnumerable<CourseSection> selectedSections)
    {
        CoreCourse = course;
        Sections = new ObservableCollection<CourseSection>(selectedSections);
    }
}