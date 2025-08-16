using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SchedulingCourse: ReactiveObject
{
    private bool _isMainCourse = true;
    private Course? _selectedReplacement;

    public Course Item { get; }
    
    public bool IsMainCourse
    {
        get => _isMainCourse;
        set => this.RaiseAndSetIfChanged(ref _isMainCourse, value);
    }
    
    public Course? SelectedReplacement
    {
        get => _selectedReplacement;
        set => this.RaiseAndSetIfChanged(ref _selectedReplacement, value);
    }
    
    public IEnumerable<Course> ReplacementOptions { get;}

    public SchedulingCourse(Course course,IEnumerable<Course> options)
    {
        Item = course;
        ReplacementOptions = options;
    }
    
}