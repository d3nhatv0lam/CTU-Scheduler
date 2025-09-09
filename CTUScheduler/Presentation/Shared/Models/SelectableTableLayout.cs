using System;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shared.Models;

public class SelectableTimetableLayout: SelectableItem<TimetableLayoutViewModel>, IEnableable, IDisposable
{
    private bool _isEnabled = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    
    public SelectableTimetableLayout(TimetableLayoutViewModel vm) : base(vm)
    {
    }

    public void Dispose()
    {
        Item?.Dispose();
    }
}