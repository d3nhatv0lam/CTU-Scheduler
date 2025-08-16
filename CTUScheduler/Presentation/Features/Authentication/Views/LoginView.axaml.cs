using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Authentication.Views;

public partial class LoginView : ReactiveUserControl<LoginViewModel>
{
    public LoginView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // one way to source
            //this.WhenAnyValue(v => v.txtUsername.Text).WhereNotNull().BindTo(ViewModel, vm => vm.UserName).DisposeWith(disposables);
            //this.WhenAnyValue(v => v.txtPassword.Text).WhereNotNull().BindTo(ViewModel, vm => vm.Password).DisposeWith(disposables);


            this.Bind<LoginViewModel, LoginView, string, string>(ViewModel, vm => vm.UserName, v => v.txtUsername.Text).DisposeWith(disposables);
            this.Bind<LoginViewModel, LoginView, string, string>(ViewModel, vm => vm.Password, v => v.txtPassword.Text).DisposeWith(disposables);
         
            this.Bind<LoginViewModel, LoginView, bool, bool?>(ViewModel, vm => vm.IsSaveUsername, x => x.chkSaveUsername.IsChecked).DisposeWith(disposables);
            this.BindCommand<LoginView, LoginViewModel, ReactiveCommand<Unit, Unit>, Button>(ViewModel, vm => vm.SignInCommand, x => x.LoginButton).DisposeWith(disposables);
        });
    }
}