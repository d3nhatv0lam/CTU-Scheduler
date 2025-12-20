namespace CTUScheduler.Core.Models.Shared;

public record OperationResult(bool IsSuccess, 
    string? ErrorMessage = null, 
    OperationFailureReason Kind = OperationFailureReason.None)
{
    public bool IsFailed => !IsSuccess;
    public static OperationResult Success() => new(true);
    public static OperationResult Failed(string errorMessage, OperationFailureReason kind) => new(false, errorMessage, kind);
}

public record OperationResult<T>(bool IsSuccess,
    T? Content = default, 
    string? ErrorMessage = null, 
    OperationFailureReason Kind = OperationFailureReason.None)
    : OperationResult(IsSuccess, ErrorMessage, Kind)
{
    public static OperationResult<T> Success(T content) => new(true, content);
    public new static OperationResult<T> Failed(string errorMessage,OperationFailureReason kind) => new(false, default, errorMessage, kind);
}