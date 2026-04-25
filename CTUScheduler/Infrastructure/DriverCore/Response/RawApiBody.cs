using CTUScheduler.Infrastructure.DriverCore.Interfaces;

namespace CTUScheduler.Infrastructure.DriverCore.Response;

public record RawApiBody<T> : IApiBody<T>
{
    public T? Data { get; init; }
    public bool IsSuccess => Data is not null;
    public T? Content => Data;
}