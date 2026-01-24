namespace CTUScheduler.Presentation.Services.ViewContext.Interfaces;

/// <summary>
/// Implement IViewContext to provide a view (top level) context service.
/// </summary>
public interface IViewContext
{
    IViewContextService ViewContext { get; }
}