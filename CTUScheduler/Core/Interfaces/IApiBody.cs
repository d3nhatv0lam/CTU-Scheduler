namespace CTUScheduler.Core.Interfaces;

public interface IApiBody<out T>
{
    bool IsSuccess { get; }
    T? Content { get; }
}