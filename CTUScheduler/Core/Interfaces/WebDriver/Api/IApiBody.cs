namespace CTUScheduler.Core.Interfaces.WebDriver.Api;

public interface IApiBody<out T>
{
    bool IsSuccess { get; }
    T? Content { get; }
}