using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetablePreviewViewModel: TimetableLayoutBaseViewModel
{
    private readonly List<SectionChoice> _choices = new ();
    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices)
    {
        if (choices is null) return;
        foreach (var choice in choices)
        {
            this.AddToGrid(choice.Course, choice.Section);
            _choices.Add(choice);
        }
    }

    public override ScheduleProfile ToModel()
    {
        return new ScheduleProfile()
        {   
            Id = Guid.NewGuid(),
            Name = this.Name,
            SavedCourseGroupKeys = _choices
                .DistinctBy(x => x.Course.Code)
                .ToDictionary(x => x.Course.Code, x => x.Section.Group),
            LastUpdated = this.LastUpdated
        };
    }
}