using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static partial class OperationResultExtensions
{
    // ==========================================================
    // SIDE EFFECTS (TAP / TAPERROR)
    // ==========================================================

    #region OperationResult Side Effects

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

    // Legacy overload
    public static OperationResult TapError(this OperationResult result, Action<IReadOnlyList<OperationError>> action)
    {
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static OperationResult TapError(this OperationResult result, Action<OperationResult> action)
    {
        if (result.IsFailed) action(result);
        return result;
    }

    // Legacy overload
    public static async Task<OperationResult> TapErrorAsync(this OperationResult result,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult> TapErrorAsync(this OperationResult result,
        Func<OperationResult, Task> action)
    {
        if (result.IsFailed) await action(result);
        return result;
    }

    #endregion

    #region OperationResult<T> Side Effects

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

    // Legacy overload
    public static OperationResult<T> TapError<T>(this OperationResult<T> result,
        Action<IReadOnlyList<OperationError>> action)
    {
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static OperationResult<T> TapError<T>(this OperationResult<T> result,
        Action<OperationResult<T>> action)
    {
        if (result.IsFailed) action(result);
        return result;
    }

    // Legacy overload
    public static async Task<OperationResult<T>> TapErrorAsync<T>(this OperationResult<T> result,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult<T>> TapErrorAsync<T>(this OperationResult<T> result,
        Func<OperationResult<T>, Task> action)
    {
        if (result.IsFailed) await action(result);
        return result;
    }

    #endregion

    #region Task<OperationResult> Side Effects

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

    // Legacy overload
    public static async Task<OperationResult> TapError(this Task<OperationResult> resultTask,
        Action<IReadOnlyList<OperationError>> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult> TapError(this Task<OperationResult> resultTask,
        Action<OperationResult> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result);
        return result;
    }

    // Legacy overload
    public static async Task<OperationResult> TapErrorAsync(this Task<OperationResult> resultTask,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult> TapErrorAsync(this Task<OperationResult> resultTask,
        Func<OperationResult, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result);
        return result;
    }

    #endregion

    #region Task<OperationResult<T>> Side Effects

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

    // Legacy overload
    public static async Task<OperationResult<T>> TapError<T>(
        this Task<OperationResult<T>> resultTask,
        Action<IReadOnlyList<OperationError>> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult<T>> TapError<T>(
        this Task<OperationResult<T>> resultTask,
        Action<OperationResult<T>> action)
    {
        var result = await resultTask;
        if (result.IsFailed) action(result);
        return result;
    }

    // Legacy overload
    public static async Task<OperationResult<T>> TapErrorAsync<T>(
        this Task<OperationResult<T>> resultTask,
        Func<IReadOnlyList<OperationError>, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result.Errors);
        return result;
    }

    // [NEW] Overload taking full result
    public static async Task<OperationResult<T>> TapErrorAsync<T>(
        this Task<OperationResult<T>> resultTask,
        Func<OperationResult<T>, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailed) await action(result);
        return result;
    }

    #endregion
}
