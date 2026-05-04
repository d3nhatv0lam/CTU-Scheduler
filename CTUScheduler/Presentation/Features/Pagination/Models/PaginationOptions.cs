using System;
using System.Collections.Generic;

namespace CTUScheduler.Presentation.Features.Pagination.Models;

public record PaginationOptions<T> where T : class
{
    public int PageSize { get; init; } = 12;
    public bool? DisposeItemsOnRemove { get; init; } = null;
    public IObservable<Func<T, bool>>? FilterObservable { get; init; } = null;
    public IObservable<IComparer<T>>? SortObservable { get; init; } = null;
}