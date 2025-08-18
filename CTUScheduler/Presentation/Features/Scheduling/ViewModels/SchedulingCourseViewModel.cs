using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels;

public class SchedulingCourseViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    public ObservableCollection<SchedulingCourse> SchedulingCourses { get; set; } = new ObservableCollection<SchedulingCourse>();

    public SchedulingCourseViewModel()
    {
        SchedulingCourses.ToObservableChangeSet()
            .AutoRefresh(x => x.IsMainCourse)
            .Subscribe(_ =>
            {
                var mainCourse = new List<SchedulingCourse>();
                var alternativeCourse = new List<SchedulingCourse>();
                foreach (var schedulingCourse in SchedulingCourses)
                {
                    if (schedulingCourse.IsMainCourse)
                        mainCourse.Add(schedulingCourse);
                    else
                        alternativeCourse.Add(schedulingCourse);
                }

                foreach (var schedulingCourse in alternativeCourse)
                {
                    schedulingCourse.ReplacementOptions = mainCourse;
                }
            })
            .DisposeWith(_disposables);
    }

    public void MapToSchedulingCourses(ReadOnlyObservableCollection<Course> courses) 
    {
        ClearSchedulingCourses();
        foreach (var course in courses)
        {
            var schedulingCourse = SchedulingCourse.CourseToSchedulingCourse(course);
            SchedulingCourses.Add(schedulingCourse);
        }
    }
    
    private void ClearSchedulingCourses()
    {
        if (SchedulingCourses.Any())
            SchedulingCourses.Clear();
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }

    
}