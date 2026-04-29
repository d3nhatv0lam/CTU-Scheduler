using System;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static partial class OperationResultExtensions
{
    // ==========================================================
    // LINQ (SELECT / SELECTMANY)
    // ==========================================================

    #region OperationResult Linq

    public static OperationResult<T> Select<T>(this OperationResult result, Func<T> selector)
    {
        return result.IsSuccess
            ? OperationResult<T>.Success(selector())
            : OperationResult<T>.FailureFrom(result);
    }

    public static async Task<OperationResult<T>> SelectAsync<T>(this OperationResult result, Func<Task<T>> selector)
    {
        if (result.IsFailed) return OperationResult<T>.FailureFrom(result);
        return OperationResult<T>.Success(await selector());
    }

    public static async ValueTask<OperationResult<T>> SelectAsync<T>(this OperationResult result,
        Func<ValueTask<T>> selector)
    {
        if (result.IsFailed) return OperationResult<T>.FailureFrom(result);
        return OperationResult<T>.Success(await selector());
    }

    // [NEW] SelectMany for non-generic
    public static OperationResult SelectMany(this OperationResult result, Func<OperationResult> nextSelector)
    {
        if (result.IsFailed) return result;
        return nextSelector();
    }

    // [NEW] SelectManyAsync for non-generic
    public static async Task<OperationResult> SelectManyAsync(this OperationResult result, Func<Task<OperationResult>> nextSelector)
    {
        if (result.IsFailed) return result;
        return await nextSelector();
    }

    #endregion

    #region OperationResult<T> Linq

    public static OperationResult<TResult> Select<T, TResult>(this OperationResult<T> result, Func<T, TResult> selector)
    {
        return result.IsSuccess
            ? OperationResult<TResult>.Success(selector(result.Content!))
            : OperationResult<TResult>.FailureFrom(result);
    }

    public static async Task<OperationResult<TResult>> SelectAsync<T, TResult>(this OperationResult<T> result,
        Func<T, Task<TResult>> selector)
    {
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static async ValueTask<OperationResult<TResult>> SelectAsync<T, TResult>(this OperationResult<T> result,
        Func<T, ValueTask<TResult>> selector)
    {
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static OperationResult<TResult> SelectMany<T, TResult>(this OperationResult<T> result,
        Func<T, OperationResult<TResult>> nextSelector)
    {
        return result.IsSuccess
            ? nextSelector(result.Content!)
            : OperationResult<TResult>.FailureFrom(result);
    }

    public static async Task<OperationResult<TResult>> SelectManyAsync<T, TResult>(this OperationResult<T> result,
        Func<T, Task<OperationResult<TResult>>> nextSelector)
    {
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return await nextSelector(result.Content!);
    }

    public static async ValueTask<OperationResult<TResult>> SelectManyAsync<T, TResult>(
        this OperationResult<T> result,
        Func<T, ValueTask<OperationResult<TResult>>> nextSelector)
    {
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return await nextSelector(result.Content!);
    }

    #endregion

    #region Task<OperationResult> Linq

    public static async Task<OperationResult<T>> Select<T>(this Task<OperationResult> resultTask, Func<T> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<T>.FailureFrom(result);
        return OperationResult<T>.Success(selector());
    }

    public static async Task<OperationResult<T>> SelectAsync<T>(this Task<OperationResult> resultTask,
        Func<Task<T>> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<T>.FailureFrom(result);
        return OperationResult<T>.Success(await selector());
    }

    public static async Task<OperationResult> SelectMany(this Task<OperationResult> resultTask,
        Func<OperationResult> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return result;
        return nextSelector();
    }

    public static async Task<OperationResult> SelectManyAsync(this Task<OperationResult> resultTask,
        Func<Task<OperationResult>> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return result;
        return await nextSelector();
    }

    #endregion

    #region Task<OperationResult<T>> Linq

    public static async Task<OperationResult<TResult>> Select<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, TResult> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return OperationResult<TResult>.Success(selector(result.Content!));
    }

    public static async Task<OperationResult<TResult>> SelectAsync<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, Task<TResult>> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static async Task<OperationResult<TResult>> SelectMany<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, OperationResult<TResult>> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return nextSelector(result.Content!);
    }

    public static async Task<OperationResult<TResult>> SelectManyAsync<T, TResult>(
        this Task<OperationResult<T>> resultTask, Func<T, Task<OperationResult<TResult>>> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.FailureFrom(result);
        return await nextSelector(result.Content!);
    }

    #endregion
}
