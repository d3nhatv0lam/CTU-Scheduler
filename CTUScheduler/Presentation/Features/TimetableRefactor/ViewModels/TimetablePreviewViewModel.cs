using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public partial class TimetablePreviewViewModel : TimetableLayoutBaseViewModel, IScorable
{
    private readonly List<SectionChoice> _choices = new();

    [Reactive] private double _totalScore;
    
    double IScorable.Score => TotalScore;

    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices, IExcelExporterService excelExporter)
        : base(excelExporter)
    {
        if (choices is null) 
        {
            SubjectsCount = 0;
            TotalCredits = 0;
            return;
        }
        _choices.AddRange(choices);

        var sourceList = new SourceList<TimetableRenderItem>()
            .DisposeWith(Disposables);
        
        var items = _choices.Select(choice =>
        {
            var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
            return CreateRenderItem(adapter);
        }).ToList();
        
        sourceList.AddRange(items);
        
        VisualizerVM = new TimetableViewModel(sourceList)
            .DisposeWith(Disposables);

        SubjectsCount = sourceList.Count;
        TotalCredits = sourceList.Items.Sum(x => x.SharedData.Credits);
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
            Name = this.Name,
            SavedCourseGroupKeys = groupKeys,
            LastUpdated = this.LastUpdated
        };
        return new ScheduleBlueprint(courses, profile);
    }


}