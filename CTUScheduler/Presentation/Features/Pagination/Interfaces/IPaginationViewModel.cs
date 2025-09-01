using System.Collections.ObjectModel;
using CTUScheduler.Core.Interfaces;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface IPaginationViewModel<T>: IPagination where T: class
{
    public ReadOnlyObservableCollection<T> PagedData { get; }
    public void AddItem(T item);
    public void Clear();
}