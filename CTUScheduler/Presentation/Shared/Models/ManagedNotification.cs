namespace CTUScheduler.Presentation.Shared.Models;

public class ManagedNotification
{
    public string? Title { get; }
    public object? Content { get; }

    public ManagedNotification(string? title, object? content)
    {
        Title = title;
        Content = content;
    }
}