using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CTUScheduler.Presentation.Features.Pagination.Models;

public sealed record PaginationOptions<T> where T : class
{
    public int PageSize { get; init; } = 12;
    public bool? DisposeItemsOnRemove { get; init; } = null;
    public IObservable<Func<T, bool>>? FilterObservable { get; init; } = null;
    public IObservable<IComparer<T>>? SortObservable { get; init; } = null;
    public IEnumerable<AutoRefreshOptions<T>>? AutoRefresh { get; init; } = null;
}

public record AutoRefreshOptions<T> where T : class
{
    public required Expression<Func<T, object?>> Property { get; init; }
    public TimeSpan? RefreshBuffer { get; init; }
}