namespace CTUScheduler.Infrastructure.DriverCore.Response;

public interface IApiBody<out T>
{
    bool IsSuccess { get; }
    T? Content { get; }
}