namespace CTUScheduler.Infrastructure.DriverCore.Interfaces;

public interface IApiBody<out T>
{
    bool IsSuccess { get; }
    T? Content { get; }
}