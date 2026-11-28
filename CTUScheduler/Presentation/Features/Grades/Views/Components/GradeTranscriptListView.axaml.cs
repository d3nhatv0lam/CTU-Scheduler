using Avalonia.Controls;
using CTUScheduler.Presentation.Features.Grades.ViewModels.Components;
using ReactiveUI.Avalonia;

namespace CTUScheduler.Presentation.Features.Grades.Views.Components;

public partial class GradeTranscriptListView : ReactiveUserControl<GradeTranscriptListViewModel>
{
    public GradeTranscriptListView()
    {
        InitializeComponent();
    }

    private void GradeDataGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Classes.Set("even", e.Row.Index % 2 == 1);
    }
}
