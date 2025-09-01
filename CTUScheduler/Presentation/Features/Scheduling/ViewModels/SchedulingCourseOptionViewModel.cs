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

public class SchedulingCourseOptionViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    public ObservableCollection<SchedulingCourse> SchedulingCourses { get; set; } = new ObservableCollection<SchedulingCourse>();

    public SchedulingCourseOptionViewModel()
    {
        SchedulingCourses.ToObservableChangeSet()
            .AutoRefresh(x => x.IsMainCourse)
            .Subscribe(_ =>
            {
                PartitionCourses(SchedulingCourses,
                    out var mainCourses,
                    out var alternativeCourses);
                
                foreach (var schedulingCourse in alternativeCourses)
                {
                    schedulingCourse.ReplacementOptions = mainCourses;
                }
            })
            .DisposeWith(_disposables);
    }
    
    private void ClearSchedulingCourses()
    {
        if (SchedulingCourses.Any())
            SchedulingCourses.Clear();
    }

    private void PartitionCourses(ObservableCollection<SchedulingCourse> schedulingCourses,out List<SchedulingCourse> mainCourses, out List<SchedulingCourse> alternativeCourses)
    {
        mainCourses = new List<SchedulingCourse>();
        alternativeCourses = new List<SchedulingCourse>();
        foreach (var schedulingCourse in SchedulingCourses)
        {
            if (schedulingCourse.IsMainCourse)
                mainCourses.Add(schedulingCourse);
            else
                alternativeCourses.Add(schedulingCourse);
        }
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

    public IEnumerable<List<Course>> GetGroupedCourses()
    {
        PartitionCourses(SchedulingCourses,
            out var mainCourses,
            out var alternativeCourses);
        
        List<List<Course>> sets = new ();
        var map = new Dictionary<Course, List<Course>>();

        // tạo group cho mainCourses
        foreach (var schedulingCourse in mainCourses)
        {
            var list = new List<Course> { schedulingCourse.Item };
            sets.Add(list);
            map[schedulingCourse.Item] = list;
        }

        foreach (var schedulingCourse in alternativeCourses)
        {
            if (schedulingCourse.SelectedReplacement == null) continue;
            if (map.TryGetValue(schedulingCourse.SelectedReplacement.Item, out var list))
            {
                list.Add(schedulingCourse.Item);
            }
        }
        return sets;
    }
    
    
    public void Dispose()
    {
        _disposables.Dispose();
    }

    
}