namespace CTUScheduler.Core.Models.Shared;

public enum OperationErrorKind
{
    None,
    Network,   
    Validation,
    NotFound,
    Unauthorized,
    System
}