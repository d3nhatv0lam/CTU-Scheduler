using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Models;
using CTUScheduler.Presentation.Shared.Interfaces;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class LimitedSelectionPaginationViewModel<T> : SelectablePaginationViewModel<T>,
    ILimitedSelectionPaginationViewModel<T>
    where T : class, ISelectable, IEnabled, INotifyPropertyChanged
{
    private readonly ObservableAsPropertyHelper<int> _maxItemCanSelect;

    public int MaxItemCanSelect => _maxItemCanSelect.Value;

    // Limit tĩnh
    public LimitedSelectionPaginationViewModel(
        int maxItemCanSelect,
        PaginationOptions<T>? options = null)
        : this(Observable.Return(maxItemCanSelect), options)
    {
    }

    // Limit động
    public LimitedSelectionPaginationViewModel(
        IObservable<int> maxItemCanSelectObservable,
        PaginationOptions<T>? options = null)
        : this(new SourceList<T>(), maxItemCanSelectObservable, options ?? new(), ownsData: true)
    {
    }
    
    // --- DÙNG CHUNG DATA ---

    // Limit tĩnh
    public LimitedSelectionPaginationViewModel(
        SourceList<T> data,
        int maxItemCanSelect,
        PaginationOptions<T>? options = null)
        : this(data, Observable.Return(maxItemCanSelect), options)
    {
    }

    // Limit động
    public LimitedSelectionPaginationViewModel(
        SourceList<T> data,
        IObservable<int> maxItemCanSelectObservable,
        PaginationOptions<T>? options = null)
        : this(data, maxItemCanSelectObservable, options ?? new(), ownsData: false)
    {
    }
    

    protected LimitedSelectionPaginationViewModel(
        SourceList<T> sourceList,
        IObservable<int> maxItemCanSelectObservable,
        PaginationOptions<T> options,
        bool ownsData)
        : base(sourceList, options, ownsData)
    {
        _maxItemCanSelect = maxItemCanSelectObservable
            .ToProperty(this, nameof(MaxItemCanSelect), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);

        // chuyển page
        PagedData.ToObservableChangeSet().Select(_ => Unit.Default)
            // có item select/unselect
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
                data.IsEnabled = !isMaxReached;
            }
        }
    }
}