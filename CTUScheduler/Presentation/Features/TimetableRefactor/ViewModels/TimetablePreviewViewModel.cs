using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetablePreviewViewModel: TimetableLayoutBaseViewModel
{
    private readonly List<SectionChoice> _choices = new ();
    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices)
    {
        if (choices is null) return;
        _choices.AddRange(choices);
        
        var sourceCache = new SourceCache<TimetableRenderItem, string>(x => x.SharedData.CourseCode)
            .DisposeWith(Disposables);
        
        foreach (var choice in _choices)
        {
            var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
            var item = CreateRenderItem(adapter);
            sourceCache.AddOrUpdate(item);
        }
        
        var stream = sourceCache.Connect().RemoveKey();
        VisualizerVM = new TimetableViewModel(stream);
            
        stream.Transform(x => x.SharedData.Credit).ToCollection()
            .Subscribe(x => TotalCredit = x.Sum()).DisposeWith(Disposables);
    }
    
    public ScheduleBlueprint ToScheduleBlueprint()
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