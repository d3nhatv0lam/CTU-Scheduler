using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

public class SchedulingCourseViewModel : ReactiveObject
{
    private bool _isMainCourse = true;
    private int _mainCourseLockCount = 0;
    private IEnumerable<SchedulingCourseViewModel> _replacementOptions = Enumerable.Empty<SchedulingCourseViewModel>();
    private SchedulingCourseViewModel? _selectedReplacement;
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

    public SchedulingCourseViewModel? SelectedReplacement
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

    public IEnumerable<SchedulingCourseViewModel> ReplacementOptions
    {
        get => _replacementOptions;
        set => this.RaiseAndSetIfChanged(ref _replacementOptions, value);
    }

    public SchedulingCourseViewModel(Course course, IEnumerable<SchedulingCourseViewModel> options)
    {
        Item = course;
        ReplacementOptions = options;
    }

    public SchedulingCourseViewModel(Course course)
    {
        Item = course;
    }
    
    public static SchedulingCourseViewModel CourseToSchedulingCourse(Course course)
    {
        return new SchedulingCourseViewModel(course);
    }
}