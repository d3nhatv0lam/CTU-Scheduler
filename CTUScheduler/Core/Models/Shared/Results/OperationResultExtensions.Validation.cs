using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static partial class OperationResultExtensions
{
    // ==========================================================
    // VALIDATION & RECOVERY (ENSURE / COMPENSATE)
    // ==========================================================

    #region OperationResult<T> Validation

    public static OperationResult<T> Ensure<T>(this OperationResult<T> result,
        Func<T, bool> predicate,
        OperationError error,
        OperationFailureReason kind = OperationFailureReason.Validation)
    {
        if (result.IsFailed) return result;
        return predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    // [NEW] EnsureAsync
    public static async Task<OperationResult<T>> EnsureAsync<T>(this OperationResult<T> result,
        Func<T, Task<bool>> predicate,
        OperationError error,
        OperationFailureReason kind = OperationFailureReason.Validation)
    {
        if (result.IsFailed) return result;
        return await predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    // Legacy overload
    public static OperationResult<T> Compensate<T>(this OperationResult<T> result,
        Func<IReadOnlyList<OperationError>, OperationResult<T>> recoveryFunc)
    {
        if (result.IsSuccess) return result;
        return recoveryFunc(result.Errors);
    }

    // [NEW] Overload taking full result
    public static OperationResult<T> Compensate<T>(this OperationResult<T> result,
        Func<OperationResult<T>, OperationResult<T>> recoveryFunc)
    {
        if (result.IsSuccess) return result;
        return recoveryFunc(result);
    }

    // [NEW] CompensateAsync
    public static async Task<OperationResult<T>> CompensateAsync<T>(this OperationResult<T> result,
        Func<OperationResult<T>, Task<OperationResult<T>>> recoveryFunc)
    {
        if (result.IsSuccess) return result;
        return await recoveryFunc(result);
    }

    #endregion

    #region Task<OperationResult<T>> Validation

    public static async Task<OperationResult<T>> Ensure<T>(this Task<OperationResult<T>> resultTask,
        Func<T, bool> predicate, OperationError error, OperationFailureReason kind = OperationFailureReason.Validation)
    {
        var result = await resultTask;
        if (result.IsFailed) return result;
        return predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    // [NEW] EnsureAsync
    public static async Task<OperationResult<T>> EnsureAsync<T>(this Task<OperationResult<T>> resultTask,
        Func<T, Task<bool>> predicate, OperationError error, OperationFailureReason kind = OperationFailureReason.Validation)
    {
        var result = await resultTask;
        if (result.IsFailed) return result;
        return await predicate(result.Content!) ? result : OperationResult<T>.Failed(error, kind);
    }

    // Legacy overload
    public static async Task<OperationResult<T>> Compensate<T>(this Task<OperationResult<T>> resultTask,
        Func<IReadOnlyList<OperationError>, OperationResult<T>> recoveryFunc)
    {
        var result = await resultTask;
        if (result.IsSuccess) return result;
        return recoveryFunc(result.Errors);
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult<T>> Compensate<T>(this Task<OperationResult<T>> resultTask,
        Func<OperationResult<T>, OperationResult<T>> recoveryFunc)
    {
        var result = await resultTask;
        if (result.IsSuccess) return result;
        return recoveryFunc(result);
    }

    // [NEW] CompensateAsync
    public static async Task<OperationResult<T>> CompensateAsync<T>(this Task<OperationResult<T>> resultTask,
        Func<OperationResult<T>, Task<OperationResult<T>>> recoveryFunc)
    {
        var result = await resultTask;
        if (result.IsSuccess) return result;
        return await recoveryFunc(result);
    }

    #endregion
}
