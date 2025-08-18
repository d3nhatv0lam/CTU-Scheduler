using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SchedulingCourse : ReactiveObject
{
    private bool _isMainCourse = true;
    private int _mainCourseLockCount = 0;
    private IEnumerable<SchedulingCourse> _replacementOptions = Enumerable.Empty<SchedulingCourse>();
    private SchedulingCourse? _selectedReplacement;
    public Course Item { get; }

    public bool IsMainCourse
    {
        get => _isMainCourse;
        set
        {
            this.RaiseAndSetIfChanged(ref _isMainCourse, value);
            if (_isMainCourse && SelectedReplacement != null)
                SelectedReplacement = null;
        }
    }

    public int MainCourseLockCount
    {
        get => _mainCourseLockCount;
        set => this.RaiseAndSetIfChanged(ref _mainCourseLockCount, value);
    }

    public SchedulingCourse? SelectedReplacement
    {
        get => _selectedReplacement;
        set
        {
            if (_selectedReplacement != null)
                _selectedReplacement.MainCourseLockCount--;
            if (value != null)
                value.MainCourseLockCount++;
            this.RaiseAndSetIfChanged(ref _selectedReplacement, value);
        }
    }

    public IEnumerable<SchedulingCourse> ReplacementOptions
    {
        get => _replacementOptions;
        set => this.RaiseAndSetIfChanged(ref _replacementOptions, value);
    }

    public SchedulingCourse(Course course, IEnumerable<SchedulingCourse> options)
    {
        Item = course;
        ReplacementOptions = options;
    }

    public SchedulingCourse(Course course)
    {
        Item = course;
    }
    
    public static SchedulingCourse CourseToSchedulingCourse(Course course)
    {
        return new SchedulingCourse(course);
    }
}