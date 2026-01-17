using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared;

public record OperationResult
{
    protected static readonly string DEFAULT_ERROR_MESSAGE = "Unknown Error";

    protected OperationResult(bool isSuccess, string? errorMessage, OperationFailureReason kind, Exception? exception)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Kind = kind;
        Exception = exception;
    }

    public void Deconstruct(
        out bool isSuccess,
        out string? errorMessage,
        out OperationFailureReason kind)
    {
        isSuccess = IsSuccess;
        errorMessage = ErrorMessage;
        kind = Kind;
    }

    public bool IsSuccess { get; }
    public bool IsFailed => !IsSuccess;
    public string? ErrorMessage { get; }
    public OperationFailureReason Kind { get; }
    public Exception? Exception { get; }

    public static OperationResult Success()
        => new(true, null, OperationFailureReason.None, null);

    public static OperationResult Failed(string errorMessage,
        OperationFailureReason kind = OperationFailureReason.System, Exception? ex = null)
        => new(false, errorMessage, kind, ex);

    public static OperationResult FromException(Exception ex,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, ex.Message, kind, ex);

    // --- MATCH METHODS (SYNC) ---
    public void Match(Action onSuccess, Action<string, OperationFailureReason, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
    }

    public R Match<R>(Func<R> onSuccess, Func<string, OperationFailureReason, Exception?, R> onFailure)
        => IsSuccess ? onSuccess() : onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);

    // --- MATCH METHODS (ASYNC) ---
    public async Task MatchAsync(Func<Task> onSuccess, Func<string, OperationFailureReason, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess();
        else await onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
    }

    public async Task<R> MatchAsync<R>(Func<Task<R>> onSuccess,
        Func<string, OperationFailureReason, Exception?, Task<R>> onFailure)
        => IsSuccess ? await onSuccess() : await onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
}

public record OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? content, string? errorMessage, OperationFailureReason kind,
        Exception? exception)
        : base(isSuccess, errorMessage, kind, exception)
    {
        Content = content;
    }

    public void Deconstruct(out bool success, out T? content, out string? error, out OperationFailureReason kind)
    {
        success = IsSuccess;
        content = Content;
        error = ErrorMessage;
        kind = Kind;
    }

    public T? Content { get; }

    // Implicit Operator: Tự động đóng gói T thành Result<T>
    // Chỉ dùng cái này nếu hiểu rõ implicit casting
    //"return course;" thay vì "return OperationResult<Course>.Success(course);"
    public static implicit operator OperationResult<T>(T content)
        => content is not null
            ? Success(content)
            : Failed("Data is null", OperationFailureReason.Validation);

    public static OperationResult<T> Success(T content)
        => new(true, content, null, OperationFailureReason.None, null);

    public new static OperationResult<T> Failed(string errorMessage,
        OperationFailureReason kind = OperationFailureReason.System, Exception? ex = null)
        => new(false, default, errorMessage, kind, ex);

    public new static OperationResult<T> FromException(Exception ex,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, default, ex.Message, kind, ex);

    // --- MATCH METHODS (SYNC) ---
    public void Match(Action<T> onSuccess, Action<string, OperationFailureReason, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess(Content!);
        else onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
    }

    public R Match<R>(Func<T, R> onSuccess, Func<string, OperationFailureReason, Exception?, R> onFailure)
        => IsSuccess ? onSuccess(Content!) : onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);

    // --- MATCH METHODS (ASYNC) ---
    public async Task MatchAsync(Func<T, Task> onSuccess,
        Func<string, OperationFailureReason, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess(Content!);
        else await onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
    }
    
    public async Task<R> MatchAsync<R>(Func<T, Task<R>> onSuccess,
        Func<string, OperationFailureReason, Exception?, Task<R>> onFailure)
    {
        if (IsSuccess) return await onSuccess(Content!);
        return await onFailure(ErrorMessage ?? DEFAULT_ERROR_MESSAGE, Kind, Exception);
    }
    
    // --- EXTENSION METHODS ---
    public OperationResult<T> Ensure(Func<T, bool> predicate, string errorMessage,
        OperationFailureReason kind = OperationFailureReason.Validation)
    {
        if (IsFailed) return this;

        return predicate(Content!)
            ? this
            : Failed(errorMessage, kind);
    }

    public OperationResult<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        if (IsFailed)
            return OperationResult<TResult>.Failed(ErrorMessage!, Kind, Exception);
        try
        {
            return OperationResult<TResult>.Success(selector(Content!));
        }
        catch (Exception ex)
        {
            return OperationResult<TResult>.FromException(ex, OperationFailureReason.System);
        }
    }

    public async Task<OperationResult<TResult>> Select<TResult>(Func<T, Task<TResult>> selector)
    {
        if (IsFailed)
            return OperationResult<TResult>.Failed(ErrorMessage!, Kind, Exception);
        try
        {
            var result = await selector(Content!);
            return OperationResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return OperationResult<TResult>.FromException(ex, OperationFailureReason.System);
        }
    }

    public OperationResult<TResult> SelectMany<TResult>(Func<T, OperationResult<TResult>> nextSelector)
    {
        if (IsFailed) return OperationResult<TResult>.Failed(ErrorMessage!, Kind, Exception);
        return  nextSelector(Content!);
    }
    public async Task<OperationResult<TResult>> SelectMany<TResult>(Func<T, Task<OperationResult<TResult>>> nextSelector)
    {
        if (IsFailed) return OperationResult<TResult>.Failed(ErrorMessage!, Kind, Exception);
        return await nextSelector(Content!);
    }
}