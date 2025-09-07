using System.Collections.Generic;
using System.Collections.ObjectModel;
using CTUScheduler.Core.Interfaces;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface IPaginationViewModel<T>: IPagination where T: class
{
    /// <summary>
    /// Data that has been added in the current time, not reactive.
    /// </summary>
    public IEnumerable<T> CurrentData { get; }
    /// <summary>
    /// Reactive data that has been added and paged.
    /// </summary>
    public ReadOnlyObservableCollection<T> PagedData { get; }
    public void AddItem(T item);
    public void Clear();
}