using System.Runtime.CompilerServices;

namespace CTUScheduler.Core.Models.Shared.Results;

public record OperationError(
    string Code,
    string DefaultMessage,
    string? Property = null,
    object[]? Args = null)
{
    public string FormattedMessage => Args is { Length: > 0} 
        ? string.Format(DefaultMessage, Args) 
        : DefaultMessage; 
    
    public override string ToString() => $"[{Code}] {FormattedMessage} (Property: {Property})";
}