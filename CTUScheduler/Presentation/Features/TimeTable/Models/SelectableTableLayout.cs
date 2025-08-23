using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Scheduling.Helpers;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;
using CTUScheduler.Presentation.Features.TimeTable.Views;

namespace CTUScheduler.Presentation.Features.TimeTable.Models;

public class SelectableTimetableLayout: SelectableItem<TimeTableLayoutViewModel>
{
    public SelectableTimetableLayout(TimeTableLayoutViewModel vm) : base(vm)
    {
    }
}