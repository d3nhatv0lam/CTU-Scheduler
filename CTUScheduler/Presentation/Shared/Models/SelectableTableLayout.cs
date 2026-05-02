using System;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shared.Models;

public class SelectableTimetableLayout: SelectableItem<TimetablePreviewViewModel>, IEnabled, IDisposable
{
    private bool _isEnabled = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    
    public SelectableTimetableLayout(TimetablePreviewViewModel vm) : base(vm)
    {
    }

    public void Dispose()
    {
        Item.Dispose();
    }
}