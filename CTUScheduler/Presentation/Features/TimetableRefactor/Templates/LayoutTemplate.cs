using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Views;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Templates;

public class LayoutTemplate: IDataTemplate
{
    public bool Match(object? data)
    {
        // return data is TimetablePreviewViewModel || data is TimetableEditorViewModel;
        return data is TimetableLayoutBaseViewModel;
    }
    
    public Control? Build(object? param)
    {
        return new TimetableLayoutView();
    }
}