using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static partial class OperationResultExtensions
{
    // ==========================================================
    // MATCH (SYNC & ASYNC)
    // ==========================================================

    #region OperationResult Match

    public static void Match(this OperationResult result,
        Action onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure,
        Action<Exception>? onException = null)
    {
        if (result.IsSuccess) onSuccess();
        else if (result.Exception != null && onException != null) onException(result.Exception);
        else onFailure(result.Errors, result.Kind);
    }

    public static R Match<R>(this OperationResult result,
        Func<R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure,
        Func<Exception, R>? onException = null)
    {
        if (result.IsSuccess) return onSuccess();
        if (result.Exception != null && onException != null) return onException(result.Exception);
        return onFailure(result.Errors, result.Kind);
    }

    public static async Task MatchAsync(this OperationResult result,
        Func<Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure,
        Func<Exception, Task>? onException = null)
    {
        if (result.IsSuccess) await onSuccess();
        else if (result.Exception != null && onException != null) await onException(result.Exception);
        else await onFailure(result.Errors, result.Kind);
    }

    public static async Task<R> MatchAsync<R>(this OperationResult result,
        Func<Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task<R>> onFailure,
        Func<Exception, Task<R>>? onException = null)
    {
        if (result.IsSuccess) return await onSuccess();
        if (result.Exception != null && onException != null) return await onException(result.Exception);
        return await onFailure(result.Errors, result.Kind);
    }

    #endregion

    #region OperationResult<T> Match

    public static void Match<T>(this OperationResult<T> result,
        Action<T> onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure,
        Action<Exception>? onException = null)
    {
        if (result.IsSuccess) onSuccess(result.Content!);
        else if (result.Exception != null && onException != null) onException(result.Exception);
        else onFailure(result.Errors, result.Kind);
    }

    public static R Match<T, R>(this OperationResult<T> result,
        Func<T, R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure,
        Func<Exception, R>? onException = null)
    {
        if (result.IsSuccess) return onSuccess(result.Content!);
        if (result.Exception != null && onException != null) return onException(result.Exception);
        return onFailure(result.Errors, result.Kind);
    }

    public static async Task MatchAsync<T>(this OperationResult<T> result,
        Func<T, Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure,
        Func<Exception, Task>? onException = null)
    {
        if (result.IsSuccess) await onSuccess(result.Content!);
        else if (result.Exception != null && onException != null) await onException(result.Exception);
        else await onFailure(result.Errors, result.Kind);
    }

    public static async Task<R> MatchAsync<T, R>(this OperationResult<T> result,
        Func<T, Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task<R>> onFailure,
        Func<Exception, Task<R>>? onException = null)
    {
        if (result.IsSuccess) return await onSuccess(result.Content!);
        if (result.Exception != null && onException != null) return await onException(result.Exception);
        return await onFailure(result.Errors, result.Kind);
    }

    #endregion

    #region Task<OperationResult> Match

    public static async Task MatchAsync(this Task<OperationResult> resultTask,
        Action onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure,
        Action<Exception>? onException = null)
    {
        var result = await resultTask;
        result.Match(onSuccess, onFailure, onException);
    }

    public static async Task<R> MatchAsync<R>(this Task<OperationResult> resultTask,
        Func<R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure,
        Func<Exception, R>? onException = null)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure, onException);
    }

    public static async Task MatchAsync(this Task<OperationResult> resultTask,
        Func<Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure,
        Func<Exception, Task>? onException = null)
    {
        var result = await resultTask;
        await result.MatchAsync(onSuccess, onFailure, onException);
    }

    public static async Task<R> MatchAsync<R>(this Task<OperationResult> resultTask,
        Func<Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task<R>> onFailure,
        Func<Exception, Task<R>>? onException = null)
    {
        var result = await resultTask;
        return await result.MatchAsync(onSuccess, onFailure, onException);
    }

    #endregion

    #region Task<OperationResult<T>> Match

    public static async Task MatchAsync<T>(this Task<OperationResult<T>> resultTask,
        Action<T> onSuccess,
        Action<IReadOnlyList<OperationError>, OperationFailureReason> onFailure,
        Action<Exception>? onException = null)
    {
        var result = await resultTask;
        result.Match(onSuccess, onFailure, onException);
    }

    public static async Task<R> MatchAsync<T, R>(this Task<OperationResult<T>> resultTask,
        Func<T, R> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, R> onFailure,
        Func<Exception, R>? onException = null)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure, onException);
    }

    public static async Task MatchAsync<T>(this Task<OperationResult<T>> resultTask,
        Func<T, Task> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task> onFailure,
        Func<Exception, Task>? onException = null)
    {
        var result = await resultTask;
        await result.MatchAsync(onSuccess, onFailure, onException);
    }

    public static async Task<R> MatchAsync<T, R>(this Task<OperationResult<T>> resultTask,
        Func<T, Task<R>> onSuccess,
        Func<IReadOnlyList<OperationError>, OperationFailureReason, Task<R>> onFailure,
        Func<Exception, Task<R>>? onException = null)
    {
        var result = await resultTask;
        return await result.MatchAsync(onSuccess, onFailure, onException);
    }

    #endregion
}
