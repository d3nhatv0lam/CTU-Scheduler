using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SelectableTimeTablesPaginationUi: PaginationViewModel<SelectableTimetableLayout>
{
    private readonly ObservableAsPropertyHelper<int> _maxPageCanSelect;
    private readonly ObservableAsPropertyHelper<int> _selectedTimetableCount;
    
    private readonly BehaviorSubject<IReadOnlyCollection<SelectableTimetableLayout>> _selectedTimetablesSubject;
    
    public int MaxItemCanSelect => _maxPageCanSelect?.Value ?? int.MaxValue;
    public int SelectedTimetableCount => _selectedTimetableCount?.Value ?? 0;
    
    public IObservable<IChangeSet<SelectableTimetableLayout>> SelectedTimetableChanged { get; }
    public IObservable<int> SelectedTimetableCountChanged { get; }
       
    
    public SelectableTimeTablesPaginationUi(int pageSize, int maxItemCanSelect) 
        : this(pageSize, Observable.Return(maxItemCanSelect)) 
    {
    }
    
    public SelectableTimeTablesPaginationUi(int pageSize, IObservable<int> maxItemCanSelectObservable) : base(pageSize)
    {
        _selectedTimetablesSubject = new BehaviorSubject<IReadOnlyCollection<SelectableTimetableLayout>>(Array.Empty<SelectableTimetableLayout>())
            .DisposeWith(Disposables);
        
        _maxPageCanSelect = maxItemCanSelectObservable
            .ToProperty(this, nameof(MaxItemCanSelect), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
        
        var selectedItemsStream = DataSharedConnection
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Publish()
            .RefCount();

        SelectedTimetableChanged = selectedItemsStream;
        
        selectedItemsStream
            .ToCollection()
            .Subscribe(_selectedTimetablesSubject)
            .DisposeWith(Disposables);
        
        SelectedTimetableCountChanged = selectedItemsStream
            .Count()
            .StartWith(0);
        
        _selectedTimetableCount = SelectedTimetableCountChanged
            .ToProperty(this, nameof(SelectedTimetableCount), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
        
      
        PagedData.ToObservableChangeSet().Select(_ => Unit.Default)
            .Merge(SelectedTimetableCountChanged.Select(_ => Unit.Default))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateItemsEnableState())
            .DisposeWith(Disposables);
    }
    
    private void UpdateItemsEnableState()
    {
        bool isMaxReached = SelectedTimetableCount >= MaxItemCanSelect;
        
        foreach (var data in PagedData)
        {
            if (!data.IsSelected)
            {
                data.IsEnabled = !isMaxReached;
            }
        }
    }
    
    public IReadOnlyCollection<SelectableTimetableLayout> GetSelectedTimetables()
    {
        return _selectedTimetablesSubject.Value;
    }
}