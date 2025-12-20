namespace CTUScheduler.Core.Models.Shared;

public enum OperationFailureReason
{
    None,
    Network,   
    Validation,
    NotFound,
    Unauthorized,
    System
}