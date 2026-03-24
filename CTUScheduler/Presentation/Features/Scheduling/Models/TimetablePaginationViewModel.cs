using System;
using CTUScheduler.Presentation.Features.Pagination.Models;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class TimetablePaginationViewModel : LimitedSelectionPaginationViewModel<SelectableTimetableLayout>
{
    public TimetablePaginationViewModel(int maxItemCanSelect,
        PaginationOptions<SelectableTimetableLayout>? options = null) : base(maxItemCanSelect, options)
    {
    }

    public TimetablePaginationViewModel(IObservable<int> maxItemCanSelectObservable,
        PaginationOptions<SelectableTimetableLayout>? options = null) : base(maxItemCanSelectObservable, options)
    {
    }

    public TimetablePaginationViewModel(SourceList<SelectableTimetableLayout> data, int maxItemCanSelect,
        PaginationOptions<SelectableTimetableLayout>? options = null) : base(data, maxItemCanSelect, options)
    {
    }

    public TimetablePaginationViewModel(SourceList<SelectableTimetableLayout> data,
        IObservable<int> maxItemCanSelectObservable,
        PaginationOptions<SelectableTimetableLayout>? options = null) : base(data, maxItemCanSelectObservable, options)
    {
    }

    protected TimetablePaginationViewModel(SourceList<SelectableTimetableLayout> sourceList,
        IObservable<int> maxItemCanSelectObservable, PaginationOptions<SelectableTimetableLayout> options,
        bool ownsData) : base(sourceList, maxItemCanSelectObservable, options, ownsData)
    {
    }
}