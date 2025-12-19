namespace CTUScheduler.Core.Models.Shared;

public record OperationResult(bool IsSuccess, 
    string? ErrorMessage = null, 
    OperationErrorKind Kind = OperationErrorKind.None)
{
    public bool IsFailed => !IsSuccess;
    public static OperationResult Success() => new(true);
    public static OperationResult Failed(string errorMessage, OperationErrorKind kind) => new(false, errorMessage, kind);
}

public record OperationResult<T>(bool IsSuccess,
    T? Content = default, 
    string? ErrorMessage = null, 
    OperationErrorKind Kind = OperationErrorKind.None)
    : OperationResult(IsSuccess, ErrorMessage,Kind)
{
    public static OperationResult<T> Success(T content) => new(true, content);
    public new static OperationResult<T> Failed(string errorMessage,OperationErrorKind kind) => new(false, default, errorMessage, kind);
}