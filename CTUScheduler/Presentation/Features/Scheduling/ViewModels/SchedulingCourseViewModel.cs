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
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels;

public class SchedulingCourseViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    public ObservableCollection<SchedulingCourse> SchedulingCourses { get; set; } = new ObservableCollection<SchedulingCourse>();

    public SchedulingCourseViewModel()
    {
      
    }

    public void MapToSchedulingCourses(ReadOnlyObservableCollection<Course> courses) 
    {
        ClearSchedulingCourses();
        foreach (var course in courses)
        {
            var schedulingCourse = ToSchedulingCourse(course);
            SchedulingCourses.Add(schedulingCourse);
        }
    }


    private void ClearSchedulingCourses()
    {
        if (SchedulingCourses.Any())
            SchedulingCourses.Clear();
    }
    
    private SchedulingCourse ToSchedulingCourse(Course course)
    {
        SchedulingCourse schedulingCourse = new SchedulingCourse(course);
        return schedulingCourse;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    
}