using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetablePreviewViewModel: TimetableLayoutBaseViewModel
{
    private readonly List<SectionChoice> _choices = new();
    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices)
    {
        if (choices is null) return;
        _choices.AddRange(choices);
        
        var sourceList = new SourceList<TimetableRenderItem>()
            .DisposeWith(Disposables);
        
        foreach (var choice in _choices)
        {
            var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
            var item = CreateRenderItem(adapter);
            sourceList.Add(item);
        }

        VisualizerVM = new TimetableViewModel(sourceList.Connect())
            .DisposeWith(Disposables);

        SubjectsCount = sourceList.Count;
        TotalCredits = sourceList.Items
            .Sum(x => x.SharedData.Credits);
    }
    
    public override ScheduleBlueprint ToScheduleBlueprint()
    {
        int count = _choices.Count;
        var courses = new List<Course>(count);
        var groupKeys = new Dictionary<string, string>(count);
        foreach (var choice in _choices)
        {
            courses.Add(choice.Course.WithSection(choice.Section));
            var courseCode = choice.Course.Code;
            groupKeys.TryAdd(courseCode, choice.Section.Group);
        }
        var profile = new ScheduleProfile()
        {   
            Id = Guid.NewGuid(),
            Name = this.Name,
            SavedCourseGroupKeys = groupKeys,
            LastUpdated = this.LastUpdated
        };
        return new ScheduleBlueprint(courses, profile);
    }
}