using System.Reactive;
using CTUScheduler.Core.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface IPaginationInteraction: IPagination
{
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
}