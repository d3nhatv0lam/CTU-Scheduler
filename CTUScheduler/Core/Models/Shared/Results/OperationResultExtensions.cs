using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static class OperationResultExtensions
{
    // ==========================================================
    // BLOCK 1: EXTENSIONS FOR OperationResult (Non-Generic)
    // ==========================================================

    #region OperationResult Extensions

    // --- MATCH (SYNC) ---
    public static void Match(this OperationResult result,
        Action onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure)
    {
        if (result.IsSuccess) onSuccess();
        else onFailure(result.Errors, result.Kind);
    }

    public static void Match(this OperationResult result,
        Action onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason, Exception?> onFailure)
    {
        if (result.IsSuccess) onSuccess();
        else onFailure(result.Errors, result.Kind, result.Exception);
    }

    public static R Match<R>(this OperationResult result,
        Func<R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure)
        => result.IsSuccess ? onSuccess() : onFailure(result.Errors, result.Kind);

    public static R Match<R>(this OperationResult result,
        Func<R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Exception?, R> onFailure)
        => result.IsSuccess ? onSuccess() : onFailure(result.Errors, result.Kind, result.Exception);

    // --- MATCH (ASYNC) ---
    public static async Task MatchAsync(this OperationResult result,
        Func<Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure)
    {
        if (result.IsSuccess) await onSuccess();
        else await onFailure(result.Errors, result.Kind);
    }

    public static async Task MatchAsync(this OperationResult result,
        Func<Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Exception?, Task> onFailure)
    {
        if (result.IsSuccess) await onSuccess();
        else await onFailure(result.Errors, result.Kind, result.Exception);
    }

    // --- LINQ ---
    public static OperationResult<T> Select<T>(this OperationResult result, Func<T> selector)
    {
        return result.IsSuccess
            ? OperationResult<T>.Success(selector())
            : OperationResult<T>.Failed(result.Errors, result.Kind);
    }

    public static async Task<OperationResult<T>> SelectAsync<T>(this OperationResult result, Func<Task<T>> selector)
    {
        if (result.IsFailed) return OperationResult<T>.Failed(result.Errors, result.Kind);
        return OperationResult<T>.Success(await selector());
    }

    public static async ValueTask<OperationResult<T>> SelectAsync<T>(this OperationResult result,
        Func<ValueTask<T>> selector)
    {
        if (result.IsFailed) return OperationResult<T>.Failed(result.Errors, result.Kind);
        return OperationResult<T>.Success(await selector());
    }

    public static OperationResult Tap(this OperationResult result, Action action)
    {
        if (result.IsSuccess) action();
        return result;
    }

    public static async Task<OperationResult> TapAsync(this OperationResult result, Func<Task> action)
    {
        if (result.IsSuccess) await action();
        return result;
    }

    public static OperationResult TapError(this OperationResult result, Action<IReadOnlyList<OperationError>> action)
    {
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    public static async Task<OperationResult> TapErrorAsync(this OperationResult result,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    #endregion


    // ==========================================================
    // EXTENSIONS FOR OperationResult<T> (Generic)
    // ==========================================================

    #region OperationResult<T> Extensions

    public static OperationResult ToResult<T>(this OperationResult<T> result)
    {
        return result.IsSuccess 
            ? OperationResult.Success() 
            : OperationResult.Failed(result.Errors, result.Kind);
    }
    // --- MATCH (SYNC) ---
    public static void Match<T>(this OperationResult<T> result,
        Action<T> onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure)
    {
        if (result.IsSuccess) onSuccess(result.Content!);
        else onFailure(result.Errors, result.Kind);
    }

    public static void Match<T>(this OperationResult<T> result,
        Action<T> onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason, Exception?> onFailure)
    {
        if (result.IsSuccess) onSuccess(result.Content!);
        else onFailure(result.Errors, result.Kind, result.Exception);
    }

    public static R Match<T, R>(this OperationResult<T> result,
        Func<T, R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure)
        => result.IsSuccess ? onSuccess(result.Content!) : onFailure(result.Errors, result.Kind);

    public static R Match<T, R>(this OperationResult<T> result,
        Func<T, R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Exception?, R> onFailure)
        => result.IsSuccess ? onSuccess(result.Content!) : onFailure(result.Errors, result.Kind, result.Exception);

    // --- MATCH (ASYNC) ---
    public static async Task MatchAsync<T>(this OperationResult<T> result,
        Func<T, Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure)
    {
        if (result.IsSuccess) await onSuccess(result.Content!);
        else await onFailure(result.Errors, result.Kind);
    }

    public static async Task<R> MatchAsync<T, R>(this OperationResult<T> result,
        Func<T, Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Exception?, Task<R>> onFailure)
    {
        if (result.IsSuccess) return await onSuccess(result.Content!);
        return await onFailure(result.Errors, result.Kind, result.Exception);
    }

    // --- FUNCTIONAL METHODS ---
    public static OperationResult<T> Ensure<T>(this OperationResult<T> result,
        Func<T, bool> predicate,
        OperationError error,
        OperationFailureReason kind = OperationFailureReason.Validation)
    {
        if (result.IsFailed) return result;
        return predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    public static OperationResult<TResult> Select<T, TResult>(this OperationResult<T> result, Func<T, TResult> selector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return OperationResult<TResult>.Success(selector(result.Content!));
    }

    public static async Task<OperationResult<TResult>> SelectAsync<T, TResult>(this OperationResult<T> result,
        Func<T, Task<TResult>> selector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static async ValueTask<OperationResult<TResult>> SelectAsync<T, TResult>(this OperationResult<T> result,
        Func<T, ValueTask<TResult>> selector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static OperationResult<TResult> SelectMany<T, TResult>(this OperationResult<T> result,
        Func<T, OperationResult<TResult>> nextSelector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return nextSelector(result.Content!);
    }

    public static async Task<OperationResult<TResult>> SelectManyAsync<T, TResult>(this OperationResult<T> result,
        Func<T, Task<OperationResult<TResult>>> nextSelector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return await nextSelector(result.Content!);
    }

    public static async ValueTask<OperationResult<TResult>> SelectManyAsync<T, TResult>(
        this OperationResult<T> result,
        Func<T, ValueTask<OperationResult<TResult>>> nextSelector)
    {
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return await nextSelector(result.Content!);
    }

    public static OperationResult<T> Tap<T>(this OperationResult<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Content!);
        return result;
    }

    public static async Task<OperationResult<T>> TapAsync<T>(this OperationResult<T> result, Func<T, Task> func)
    {
        if (result.IsSuccess) await func(result.Content!);
        return result;
    }

    public static OperationResult<T> TapError<T>(this OperationResult<T> result,
        Action<IReadOnlyList<OperationError>> action)
    {
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    public static async Task<OperationResult<T>> TapErrorAsync<T>(this OperationResult<T> result,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    #endregion

    #region Task<OperationResult> Extensions

    public static async Task<OperationResult<T>> Select<T>(this Task<OperationResult> resultTask, Func<T> selector)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? OperationResult<T>.Success(selector())
            : OperationResult<T>.Failed(result.Errors, result.Kind);
    }

    public static async Task<OperationResult<T>> SelectAsync<T>(this Task<OperationResult> resultTask,
        Func<Task<T>> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<T>.Failed(result.Errors, result.Kind);
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

    public static async Task<OperationResult> Tap(this Task<OperationResult> resultTask, Action action)
    {
        var result = await resultTask;
        if (result.IsSuccess) action();
        return result;
    }

    public static async Task<OperationResult> TapAsync(this Task<OperationResult> resultTask, Func<Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) await action();
        return result;
    }

    public static async Task<OperationResult> TapError(this Task<OperationResult> resultTask,
        Action<IReadOnlyList<OperationError>> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    public static async Task<OperationResult> TapErrorAsync(this Task<OperationResult> resultTask,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result.Errors);
        return result;
    }
    
    public static OperationResult<T> Compensate<T>(this OperationResult<T> result, 
        Func<IReadOnlyList<OperationError>, OperationResult<T>> recoveryFunc)
    {
        if (result.IsSuccess) return result;
        return recoveryFunc(result.Errors);
    }

    #endregion

    #region Task<OperationResult<T>> Extensions

    public static async Task<OperationResult> ToResult<T>(this Task<OperationResult<T>> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess 
            ? OperationResult.Success() 
            : OperationResult.Failed(result.Errors, result.Kind);
    }
    
    public static async Task<OperationResult<T>> Tap<T>(
        this Task<OperationResult<T>> resultTask,
        Action<T> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) action(result.Content!);
        return result;
    }

    public static async Task<OperationResult<T>> TapAsync<T>(
        this Task<OperationResult<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) await action(result.Content!);
        return result;
    }

    public static async Task<OperationResult<T>> TapError<T>(
        this Task<OperationResult<T>> resultTask,
        Action<IReadOnlyList<OperationError>> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    public static async Task<OperationResult<T>> TapErrorAsync<T>(
        this Task<OperationResult<T>> resultTask,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    public static async Task<OperationResult<T>> Ensure<T>(this Task<OperationResult<T>> resultTask,
        Func<T, bool> predicate, OperationError error, OperationFailureReason kind = OperationFailureReason.Validation)
    {
        var result = await resultTask;
        if (result.IsFailed) return result;
        return predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    public static async Task<OperationResult<TResult>> Select<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, TResult> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return OperationResult<TResult>.Success(selector(result.Content!));
    }

    public static async Task<OperationResult<TResult>> SelectAsync<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, Task<TResult>> selector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return OperationResult<TResult>.Success(await selector(result.Content!));
    }

    public static async Task<OperationResult<TResult>> SelectMany<T, TResult>(this Task<OperationResult<T>> resultTask,
        Func<T, OperationResult<TResult>> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return nextSelector(result.Content!);
    }

    public static async Task<OperationResult<TResult>> SelectManyAsync<T, TResult>(
        this Task<OperationResult<T>> resultTask, Func<T, Task<OperationResult<TResult>>> nextSelector)
    {
        var result = await resultTask;
        if (result.IsFailed) return OperationResult<TResult>.Failed(result.Errors, result.Kind);
        return await nextSelector(result.Content!);
    }

    public static async Task MatchAsync<T>(this Task<OperationResult<T>> resultTask,
        Action<T> onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure)
    {
        var result = await resultTask;
        result.Match(onSuccess, onFailure);
    }
    
    public static async Task MatchAsync<T>(this Task<OperationResult<T>> resultTask,
        Func<T, Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure)
    {
        var result = await resultTask;
        if (result.IsSuccess) await onSuccess(result.Content!);
        else await onFailure(result.Errors, result.Kind);
    }
    
    public static async Task<R> MatchAsync<T, R>(this Task<OperationResult<T>> resultTask,
        Func<T, R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }
    
    public static async Task<R> MatchAsync<T, R>(this Task<OperationResult<T>> resultTask,
        Func<T, Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task<R>> onFailure)
    {
        var result = await resultTask;
        if (result.IsSuccess) return await onSuccess(result.Content!);
        return await onFailure(result.Errors, result.Kind);
    }

    #endregion
}