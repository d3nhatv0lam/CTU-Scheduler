using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.AppShell.ViewModels;

public class MainViewModel : ViewModelBase , IScreen , IActivatableViewModel
{
    private readonly IConnectivityService _connectivityService;
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
        
        _connectivityService = App.ServiceProvider.GetRequiredService<IConnectivityService>();
        Router = new RoutingState();
        Router.Navigate.Execute(new LoginViewModel(this));
        // Router.Navigate.Execute(new MainShellViewModel(this));

        this.WhenActivated(disposables =>
        {
            _connectivityService.IsInternetAvailable
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isAvailable =>
                {
                    WindowTitle = isAvailable ? "CTU Scheduler" : "CTU Scheduler - No Internet";
                }).DisposeWith(disposables);
        });
    }
}
