using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.AppServices.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;
using System.Diagnostics;
using System.Reactive;
using CTUScheduler.Presentation.ViewModels.HomePage;
using CTUScheduler.Presentation.ViewModels.Shells.Components;
using CTUScheduler.AppServices.Services.Implementations;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Sign;

namespace CTUScheduler.Presentation.ViewModels.Shells;

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
        Router.Navigate.Execute(new SignInViewModel(this));
        //Router.Navigate.Execute(new MainLayoutViewModel(this));

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
