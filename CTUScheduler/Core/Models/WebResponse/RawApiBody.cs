using CTUScheduler.Core.Interfaces.WebDriver.Api;

namespace CTUScheduler.Core.Models.WebResponse;

public record RawApiBody<T> : IApiBody<T>
{
    public T? Data { get; init; }
    public bool IsSuccess => Data is not null;
    public T? Content => Data;
}