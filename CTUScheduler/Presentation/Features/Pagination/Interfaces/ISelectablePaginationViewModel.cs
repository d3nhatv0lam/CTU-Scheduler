using System;
using System.Collections.Generic;
using System.ComponentModel;
using CTUScheduler.Presentation.Shared.Interfaces;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface ISelectablePaginationViewModel<T> : IPaginationViewModel<T>
    where T : class, ISelectable, INotifyPropertyChanged
{
    int SelectedItemCount { get; }
    IObservable<IChangeSet<T>> SelectedItemChanged { get; }
    IObservable<int> SelectedItemCountChanged { get; }
    IReadOnlyCollection<T> GetSelectedItems();
}