namespace RxAmethyst.PlaywrightWrapper.Models;

public record RawApiBody<T>
{
    public bool IsSuccess => Content is not null;
    public T? Content { get; init; }
}