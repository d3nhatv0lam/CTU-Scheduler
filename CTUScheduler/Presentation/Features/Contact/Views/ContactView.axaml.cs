using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Contact.ViewModels;

namespace CTUScheduler.Presentation.Features.Contact.Views;

public partial class ContactView : ReactiveUserControl<ContactViewModel>
{
    public ContactView()
    {
        InitializeComponent();
    }
}
