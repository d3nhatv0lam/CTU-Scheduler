using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;


namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class SelectablePaginationViewModel<T> : PaginationViewModel<T> , ISelectablePaginationViewModel<T>
where T : class, ISelectable , IActivatable, INotifyPropertyChanged
{
    private readonly ObservableAsPropertyHelper<int> _maxItemCanSelect;
    private readonly ObservableAsPropertyHelper<int> _selectedItemCount;
    
    private readonly BehaviorSubject<IReadOnlyCollection<T>> _selectedItemsSubject;
    
    public int MaxItemCanSelect => _maxItemCanSelect?.Value ?? int.MaxValue;
    public int SelectedItemCount => _selectedItemCount?.Value ?? 0;
    
    public IObservable<IChangeSet<T>> SelectedItemChanged { get; }
    public IObservable<int> SelectedItemCountChanged { get; }
       
    public SelectablePaginationViewModel(int pageSize, int maxItemCanSelect) 
        : this(pageSize, Observable.Return(maxItemCanSelect)) 
    {
    }
    
    public SelectablePaginationViewModel(int pageSize, IObservable<int> maxItemCanSelectObservable) : base(pageSize)
    {
        _selectedItemsSubject = new BehaviorSubject<IReadOnlyCollection<T>>(Array.Empty<T>())
            .DisposeWith(Disposables);
        
        _maxItemCanSelect = maxItemCanSelectObservable
            .ToProperty(this, nameof(MaxItemCanSelect), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
        
        var selectedItemsStream = DataSharedConnection
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Publish()
            .RefCount();

        SelectedItemChanged = selectedItemsStream;
        
        selectedItemsStream
            .ToCollection()
            .Subscribe(_selectedItemsSubject)
            .DisposeWith(Disposables);
        
        SelectedItemCountChanged = selectedItemsStream
            .Count()
            .StartWith(0);
        
        _selectedItemCount = SelectedItemCountChanged
            .ToProperty(this, nameof(SelectedItemCount), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
      
        PagedData.ToObservableChangeSet().Select(_ => Unit.Default)
            .Merge(SelectedItemCountChanged.Select(_ => Unit.Default))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateItemsEnableState())
            .DisposeWith(Disposables);
    }
    
    private void UpdateItemsEnableState()
    {
        bool isMaxReached = SelectedItemCount >= MaxItemCanSelect;
        
        foreach (var data in PagedData)
        {
            if (!data.IsSelected)
            {
                // Nhờ có ISelectableItem, ta có thể set IsEnabled an toàn
                data.IsEnabled = !isMaxReached;
            }
        }
    }
    
    public IReadOnlyCollection<T> GetSelectedItems()
    {
        return _selectedItemsSubject.Value;
    }
}