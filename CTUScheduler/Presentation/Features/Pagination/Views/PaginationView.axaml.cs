using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;

namespace CTUScheduler.Presentation.Features.Pagination.Views;

public partial class PaginationView : ReactiveUserControl<IPaginationInteraction>{
    public PaginationView()
    {
        InitializeComponent();
    }
}