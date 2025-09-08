using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SelectableTimeTablesPaginationUi: PaginationViewModel<SelectableTimetableLayout>
{
    private int _selectedTimetableCount;
    private int _maxPageCanSelect;
    public int SelectedTimetableCount { 
        get => _selectedTimetableCount;
        set => this.RaiseAndSetIfChanged(ref _selectedTimetableCount, value);
    }
    public int MaxPageCanSelect
    {
        get => _maxPageCanSelect;
        init => this.RaiseAndSetIfChanged(ref _maxPageCanSelect, value);
    }
    
    public IObservable<int> SelectedTimetableCountChanged { get; private set; } = null!;
       
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SelectableTimeTablesPaginationUi(int pageSize, int maxPageCanSelect) : base(new SourceList<SelectableTimetableLayout>(), pageSize)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        MaxPageCanSelect = maxPageCanSelect;
    }

    protected override void OnObservableInit()
    {
        
        base.OnObservableInit();
        SelectedTimetableCountChanged = DataSharedConnection
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Count()
            .Do(x => Debug.WriteLine("Count Changed: {0}", x))
            .Replay(1) 
            .RefCount();
        
        SelectedTimetableCountChanged
            .Do(x => Debug.WriteLine("new cout: {0}", x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(count => SelectedTimetableCount = count)
            .DisposeWith(Disposables);

        PagedData.ToObservableChangeSet().Select(_ => Unit.Default)
            .Merge(this.WhenAnyValue(x => x.SelectedTimetableCount).Select(_ => Unit.Default))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                if (SelectedTimetableCount >= MaxPageCanSelect)
                {
                    foreach (var data in PagedData)
                        if (!data.IsSelected)
                            data.IsEnabled = false;
                }
                else
                {
                    foreach (var data in PagedData)
                            data.IsEnabled = true;
                }
            })
            .DisposeWith(Disposables);
    }
}