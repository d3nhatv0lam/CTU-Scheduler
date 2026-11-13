using System;
using CTUScheduler.Core.Models.Timetable;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

public partial class AvoidanceCellViewModel : ReactiveObject
{
    public DayOfWeek Day { get; }
    public TimeOfDay Time { get; }

    [Reactive] private bool _isAvoided;

    public AvoidanceCellViewModel(DayOfWeek day, TimeOfDay time, bool isAvoided = false)
    {
        Day = day;
        Time = time;
        _isAvoided = isAvoided;
    }
}
