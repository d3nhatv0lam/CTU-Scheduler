using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Views;

public partial class SignInView : ReactiveUserControl<SignInViewModel>
{
    public SignInView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Handle any activation logic here
            this.Bind(ViewModel, vm => vm.Username, v => v.txtUsername.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Password, x => x.txtPassword.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Capcha, x => x.txtCapcha.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CapchaImage, x => x.capchaImage.Source).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.IsSaveUsername, x => x.chkSaveUsername.IsChecked).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SignInCommand, x => x.LoginButton).DisposeWith(disposables);
        });
    }
}