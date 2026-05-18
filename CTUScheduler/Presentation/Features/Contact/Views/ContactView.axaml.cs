using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;
using ReactiveUI;
using CTUScheduler.Presentation.Features.Contact.ViewModels;

namespace CTUScheduler.Presentation.Features.Contact.Views;

public partial class ContactView : ReactiveUserControl<ContactViewModel>
{
    public ContactView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
