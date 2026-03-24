using System;
using System.Collections.Generic;
using System.ComponentModel;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface ISelectablePaginationViewModel<T>: IPaginationViewModel<T> 
    where T : class, ISelectable, IActivatable, INotifyPropertyChanged
{
    public int MaxItemCanSelect { get; }
    public int SelectedItemCount { get; }
    
    public IObservable<IChangeSet<T>> SelectedItemChanged { get; }
    public IObservable<int> SelectedItemCountChanged { get; }
    
    public IReadOnlyCollection<T> GetSelectedItems();
}