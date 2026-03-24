using System.ComponentModel;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface ILimitedSelectionPaginationViewModel<T>: ISelectablePaginationViewModel<T>
where T: class, ISelectable, IEnabled, INotifyPropertyChanged
{
    int MaxItemCanSelect { get; }
}