using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SchedulingCourse: ReactiveObject
{
    private bool _isMainCourse = true;
    private bool _isMainCourseLocked = false;
    private IEnumerable<SchedulingCourse> _replacementOptions = Enumerable.Empty<SchedulingCourse>();
    private SchedulingCourse? _selectedReplacement;
    public Course Item { get; }
    public bool IsMainCourseLocked
    {
        get => _isMainCourseLocked;
        set => this.RaiseAndSetIfChanged(ref _isMainCourseLocked, value);
    }
    
    public bool IsMainCourse
    {
        get => _isMainCourse;
        set => this.RaiseAndSetIfChanged(ref _isMainCourse, value);
    }
    
    public SchedulingCourse? SelectedReplacement
    {
        get => _selectedReplacement;
        set => this.RaiseAndSetIfChanged(ref _selectedReplacement, value);
    }

    public IEnumerable<SchedulingCourse> ReplacementOptions
    {
        get => _replacementOptions;
        set => this.RaiseAndSetIfChanged(ref _replacementOptions, value);
    }

    public SchedulingCourse(Course course,IEnumerable<SchedulingCourse> options)
    {
        Item = course;
        ReplacementOptions = options;
    }
    
    public SchedulingCourse(Course course)
    {
        Item = course;
    }
    
}