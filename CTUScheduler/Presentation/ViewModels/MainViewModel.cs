using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.AppServices.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;
using System.Diagnostics;
using System.Reactive;

namespace CTUScheduler.Presentation.ViewModels;

public class MainViewModel : ViewModelBase , IScreen
{
    public string Greeting => "Welcome to Avalonia!";

    public RoutingState Router { get; }

    public MainViewModel()
    {
        Router = new RoutingState();
        //Router.Navigate.Execute(new SignInViewModel(this));
        Router.Navigate.Execute(new MainHomeViewModel(this));
    }
}
