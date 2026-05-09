using System;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Contact.ViewModels;

public class ContactViewModel : ViewModelBase, IRoutableViewModel, IViewModel
{
    public string UrlPathSegment => nameof(ContactViewModel);
    public IScreen HostScreen { get; }

    public ContactViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
    }
}
