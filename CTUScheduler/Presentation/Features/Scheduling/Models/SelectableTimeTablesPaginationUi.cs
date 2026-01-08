using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
    private readonly ObservableAsPropertyHelper<int> _maxPageCanSelect;
    
    private readonly BehaviorSubject<IReadOnlyCollection<SelectableTimetableLayout>> _selectedTimetablesSubject = 
        new(Array.Empty<SelectableTimetableLayout>());
    public int SelectedTimetableCount { 
        get => _selectedTimetableCount;
        set => this.RaiseAndSetIfChanged(ref _selectedTimetableCount, value);
    }
    public int MaxPageCanSelect => _maxPageCanSelect?.Value ?? int.MaxValue;
    
    protected IObservable<IChangeSet<SelectableTimetableLayout>> SelectedTimetableChanged { get; private set; } = null!;
    public IObservable<int> SelectedTimetableCountChanged { get; private set; } = null!;
       
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SelectableTimeTablesPaginationUi(int pageSize, int maxPageCanSelect) : base(new SourceList<SelectableTimetableLayout>(), pageSize)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _maxPageCanSelect = Observable.Return(MaxPageCanSelect).ToProperty(this, nameof(MaxPageCanSelect)).DisposeWith(Disposables);
        Disposables.Add(_selectedTimetablesSubject);
    }
    
    public SelectableTimeTablesPaginationUi(int pageSize, IObservable<int> maxPageCanSelect) : base(new SourceList<SelectableTimetableLayout>(), pageSize)
    {
        _maxPageCanSelect = maxPageCanSelect.ToProperty(this, nameof(MaxPageCanSelect))
            .DisposeWith(Disposables);
        _selectedTimetablesSubject.DisposeWith(Disposables);
    }

    protected override void OnObservableInit()
    {
        base.OnObservableInit();

        var filteredStream = DataSharedConnection
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Publish()
            .RefCount();

        // Subscribe để cập nhật BehaviorSubject từ filteredStream
        filteredStream
            .ToCollection()
            .Subscribe(_selectedTimetablesSubject);

        // Tạo các derived streams từ filteredStream chung
        SelectedTimetableChanged = filteredStream
            .Replay(1)
            .RefCount();

        SelectedTimetableCountChanged = filteredStream
            .Count()
            .Replay(1)
            .RefCount();
        
        SelectedTimetableCountChanged
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

    public async Task<IReadOnlyCollection<SelectableTimetableLayout>> GetSelectedTimetables()
    {
        return await _selectedTimetablesSubject
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(1)) // Safety net
            .Catch(Observable.Return(
                (IReadOnlyCollection<SelectableTimetableLayout>)[]
            )); // Fallback
    }
}