using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace CTUScheduler.Core.Models.Shared.Results;

public static class OperationResultRxExtensions
{
    public static IObservable<Unit> WhenSuccess(this IObservable<OperationResult> source)
    {
        return source.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default);
    }

    public static IObservable<IReadOnlyList<OperationError>> WhenFailed(this IObservable<OperationResult> source)
    {
        return source.Where(x => x.IsFailed)
            .Select(x => x.Errors);
    }

    public static IObservable<string> WhenErrorMessage(this IObservable<OperationResult> source)
    {
        return source.Where(x => x.IsFailed)
            .Select(x => x.FirstErrorMessage ?? OperationResult.DEFAULT_ERROR_MESSAGE);
    }

    // OperationResult<T>

    public static IObservable<Unit> WhenSuccessUnit<T>(this IObservable<OperationResult<T>> source)
    {
        return source.Where(x => x.IsSuccess)
            .Select(_ => Unit.Default);
    }

    public static IObservable<T> WhenSuccess<T>(this IObservable<OperationResult<T>> source)
    {
        return source.Where(x => x.IsSuccess)
            .Select(x => x.Content!);
    }

    public static IObservable<IReadOnlyList<OperationError>> WhenFailed<T>(this IObservable<OperationResult<T>> source)
    {
        return source.Where(x => x.IsFailed)
            .Select(x => x.Errors);
    }

    public static IObservable<string> WhenErrorMessage<T>(this IObservable<OperationResult<T>> source)
    {
        return source.Where(x => x.IsFailed)
            .Select(x => x.FirstErrorMessage ?? OperationResult.DEFAULT_ERROR_MESSAGE);
    }
}