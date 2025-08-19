using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public class MainViewModel : ViewModelBase , IScreen , IActivatableViewModel
{
    private readonly IInternetStatusService _internetStatusService;
    private string _windowTitle = "CTU Scheduler";
    public RoutingState Router { get; }
    public string WindowTitle
    {
        get => _windowTitle;
        set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }

    public ViewModelActivator Activator {get;}

    public MainViewModel()
    {
        Activator = new ViewModelActivator();
        
        _internetStatusService = App.ServiceProvider!.GetRequiredService<IInternetStatusService>();
        Router = new RoutingState();
        //Router.Navigate.Execute(new LoginViewModel(this));
        Router.Navigate.Execute(new MainShellViewModel(this));

        this.WhenActivated(disposables =>
        {
            // Check internet status and update window title accordingly
            _internetStatusService.InternetStatusOnRefresh
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";
                }).DisposeWith(disposables);
        });
        
    }
}
