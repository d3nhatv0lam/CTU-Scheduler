using System;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class SelectableTimeTablesPaginationUi: PaginationViewModel<SelectableTimetableLayout>
{
    public IObservable<int> SelectedPagesChanged { get; protected set; } = null!;

    public SelectableTimeTablesPaginationUi(): base()
    {
    }

    protected override void OnObservableInit()
    {
        base.OnObservableInit();
        SelectedPagesChanged = Data.Connect()
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Count();
    }

}