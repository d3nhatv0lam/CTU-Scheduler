using System;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Presentation.Features.Timetable.Interfaces;

public interface IRequestBuildTimetable
{
    event Action<TimetableLayoutViewModel>? BuildTimetableRequested;
    void BuildTimetable();
}