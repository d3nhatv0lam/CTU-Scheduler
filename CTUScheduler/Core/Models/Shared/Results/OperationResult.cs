using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CTUScheduler.Core.Models.Shared.Results;

public record OperationResult
{
    public static readonly string DEFAULT_ERROR_MESSAGE = "Unknown Error";

    [JsonConstructor]
    protected OperationResult(bool isSuccess, OperationFailureReason kind, IEnumerable<OperationError>? errors,
        Exception? exception)
    {
        IsSuccess = isSuccess;
        Kind = kind;
        Errors = errors?.ToList().AsReadOnly() ?? new List<OperationError>().AsReadOnly();
        Exception = exception;
    }

    public void Deconstruct(out bool isSuccess, out OperationFailureReason kind,
        out IReadOnlyList<OperationError> errors)
    {
        isSuccess = IsSuccess;
        kind = Kind;
        errors = Errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailed => !IsSuccess;
    public IReadOnlyList<OperationError> Errors { get; init; }
    public string? FirstErrorMessage => Errors.FirstOrDefault()?.FormattedMessage;
    public OperationFailureReason Kind { get; }
    [JsonIgnore] 
    public Exception? Exception { get; }


    public static OperationResult Success()
        => new(true, OperationFailureReason.None, null, null);

    // public static OperationResult Failed(string errorMessage,
    //     OperationFailureReason kind = OperationFailureReason.System, Exception? ex = null)
    //     => new(false, errorMessage, kind, ex);

    public static OperationResult Failed(OperationError error,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, kind, [error], null);

    public static OperationResult Failed(IEnumerable<OperationError> errors,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, kind, errors, null);

    public static OperationResult Failed(string message, string code = "General.Error",
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, kind, [new OperationError(code, message)], null);

    public static OperationResult FromException(Exception ex,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, kind, [new OperationError("System.Exception", ex.Message)], ex);

    public static OperationResult Combine(params OperationResult[] results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Length == 0) return Success();

        var failedResults = results.Where(r => r.IsFailed).ToList();
        if (failedResults.Count == 0) return Success();

        OperationFailureReason[] priorityKinds =
        [
            OperationFailureReason.Unauthorized,
            OperationFailureReason.System
        ];

        var finalKind = priorityKinds
            .FirstOrDefault(pk => failedResults.Any(fr => fr.Kind == pk));

        if (finalKind == OperationFailureReason.None)
        {
            finalKind = failedResults[0].Kind;
            
            if (finalKind == OperationFailureReason.None) 
                finalKind = OperationFailureReason.System;
        }

        var errors = failedResults.SelectMany(x => x.Errors).ToList();

        var firstException = failedResults.FirstOrDefault(r => r.Exception != null)?.Exception;
        
        return new OperationResult(false, finalKind, errors, firstException);
    }
}

public record OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? content, OperationFailureReason kind,
        IEnumerable<OperationError>? errors, Exception? exception)
        : base(isSuccess, kind, errors, exception)
    {
        Content = content;
    }

    public void Deconstruct(out bool success, out T? content, out OperationFailureReason kind,
        out IReadOnlyList<OperationError>? errors)
    {
        success = IsSuccess;
        content = Content;
        kind = Kind;
        errors = Errors;
    }

    public T? Content { get; }

    // Implicit Operator: Tự động đóng gói T thành Result<T>
    // Chỉ dùng cái này nếu hiểu rõ implicit casting
    //"return course;" thay vì "return OperationResult<Course>.Success(course);"
    public static implicit operator OperationResult<T>(T content)
        => Success(content);

    public static OperationResult<T> Success(T content)
        => new(true, content, OperationFailureReason.None, null, null);

    public new static OperationResult<T> Failed(OperationError error,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, default, kind, [error], null);

    public new static OperationResult<T> Failed(IEnumerable<OperationError> errors,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, default, kind, errors, null);

    public new static OperationResult<T> Failed(string message, string code = "General.Error",
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, default, kind, [new OperationError(code, message)], null);

    public new static OperationResult<T> FromException(Exception ex,
        OperationFailureReason kind = OperationFailureReason.System)
        => new(false, default, kind, [new OperationError("System.Exception", ex.Message)], ex);
}