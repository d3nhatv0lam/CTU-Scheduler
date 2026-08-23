using Avalonia.Controls;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Components;

public partial class AvoidanceFilterView : ReactiveUserControl<AvoidanceFilterViewModel>
{
    public AvoidanceFilterView()
    {
        InitializeComponent();
    }

    private void OnComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
        {
            comboBox.SelectedItem = null;
        }
    }
}
