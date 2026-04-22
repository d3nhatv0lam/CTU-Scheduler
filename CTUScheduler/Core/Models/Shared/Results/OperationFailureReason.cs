namespace CTUScheduler.Core.Models.Shared.Results;

public enum OperationFailureReason
{
    None,
    Network,   
    Validation,
    NotFound,
    Unauthorized,
    System,
    Database,
    UserAction
}