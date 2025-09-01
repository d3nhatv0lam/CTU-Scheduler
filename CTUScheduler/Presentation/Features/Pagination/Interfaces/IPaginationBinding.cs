using System.Reactive;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.Interfaces;

public interface IPaginationBinding
{
    public int CurrentPage { get; }
    public int TotalPages { get; }
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
}