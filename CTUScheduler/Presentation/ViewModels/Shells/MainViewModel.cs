using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.AppServices.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;
using System.Diagnostics;
using System.Reactive;
using CTUScheduler.Presentation.ViewModels.Home;
using CTUScheduler.Presentation.ViewModels.Shells.Components;

namespace CTUScheduler.Presentation.ViewModels.Shells;

public class MainViewModel : ViewModelBase , IScreen
{
    public RoutingState Router { get; }

    public MainViewModel()
    {
        Router = new RoutingState();
        //Router.Navigate.Execute(new SignInViewModel(this));
        Router.Navigate.Execute(new MainLayoutViewModel(this));
    }
}
