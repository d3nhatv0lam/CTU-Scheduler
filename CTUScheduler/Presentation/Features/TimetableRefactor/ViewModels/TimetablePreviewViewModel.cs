using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Exel;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetablePreviewViewModel: TimetableLayoutBaseViewModel
{
    private readonly List<SectionChoice> _choices = new();
    private readonly IExcelExporterService _excelExporter;

    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices, IExcelExporterService excelExporter)
    {
        _excelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));
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

        VisualizerVM = new TimetableViewModel(sourceList.Connect());

        SubjectsCount = sourceList.Count;
        TotalCredits = sourceList.Items
            .Sum(x => x.SharedData.Credits);
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

    public async Task<OperationResult<string>> ExportToExcelFileAsync(string filePath)
    {
        // Tạo column định nghĩa (tùy domain)
        var columns = new[]
        {
            new ExportColumnDefinition<SectionChoice> { Header = "Code", ValueSelector = c => c.Course?.Code },
            new ExportColumnDefinition<SectionChoice> { Header = "Name", ValueSelector = c => c.Course?.Name_VN },
            new ExportColumnDefinition<SectionChoice> { Header = "Section", ValueSelector = c => c.Section?.Group },
            new ExportColumnDefinition<SectionChoice> { Header = "Credits", ValueSelector = c => c.Course?.Credits, NumberFormat = "0" }
        };

        var options = new ExcelExportOptions { SheetName = "Timetable", AutoFitColumns = true };

        return await _excelExporter.ExportToFileAsync(_choices, filePath, columns, options);
    }
}