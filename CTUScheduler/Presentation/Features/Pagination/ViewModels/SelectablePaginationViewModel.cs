using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Models;
using CTUScheduler.Presentation.Shared.Interfaces;
using DynamicData;
using ReactiveUI;


namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class SelectablePaginationViewModel<T> : PaginationViewModel<T>, ISelectablePaginationViewModel<T>
    where T : class, ISelectable, IEnabled, INotifyPropertyChanged
{
    private readonly IObservableList<T> _selectedListCache;
    private readonly ObservableAsPropertyHelper<int> _selectedItemCount;

    public int SelectedItemCount => _selectedItemCount.Value;
    public IObservable<IChangeSet<T>> SelectedItemChanged { get; }
    public IObservable<int> SelectedItemCountChanged { get; }

    public SelectablePaginationViewModel(PaginationOptions<T>? options = null)
        : this(new SourceList<T>(), options ?? new(), ownsData: true)
    {
    }

    public SelectablePaginationViewModel(SourceList<T> data, PaginationOptions<T>? options = null)
        : this(data, options ?? new(), ownsData: false)
    {
    }

    protected SelectablePaginationViewModel(SourceList<T> sourceList, PaginationOptions<T> options, bool ownsData)
        : base(sourceList, options, ownsData)
    {
        var selectedItemsStream = DataSharedConnection
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Publish()
            .RefCount();

        SelectedItemChanged = selectedItemsStream;

        _selectedListCache = selectedItemsStream.AsObservableList()
            .DisposeWith(Disposables);

        SelectedItemCountChanged = _selectedListCache.CountChanged;

        _selectedItemCount = SelectedItemCountChanged
            .ToProperty(this, nameof(SelectedItemCount), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
    }


    public IReadOnlyCollection<T> GetSelectedItems() => _selectedListCache.Items.ToList();
}