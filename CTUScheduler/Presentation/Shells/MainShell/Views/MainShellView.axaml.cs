using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Material.Styles.Controls;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.MainShell.Views;

public partial class MainShellView : ReactiveUserControl<MainShellViewModel>
{
    public MainShellView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {

            //this.OneWayBind(ViewModel, vm => vm.NavigationItems, v => v.lBoxNavigation.ItemsSource).DisposeWith(disposables);

            //this.OneWayBind(ViewModel, vm => vm.Title, v => v.txtTitle.Text).DisposeWith(disposables);

            //this.WhenAnyValue(x => x.lBoxNavigation.SelectedItem).BindTo(ViewModel, vm => vm.SelectedItem).DisposeWith(disposables);

            //this.OneWayBind(ViewModel, vm => vm.Router, v => v.PageRounter.Router).DisposeWith(disposables);

            //Observable.FromEventPattern<EventArgs>(lBoxNavigation, nameof(lBoxNavigation.Loaded)).Subscribe(_ =>
            //{
            //    if (lBoxNavigation.Items.Count > 0)
            //        lBoxNavigation.SelectedIndex = 0;
            //}).DisposeWith(disposables);


            Observable.FromEventPattern<EventArgs>(lBoxNavigation, nameof(InputElement.PointerReleased)).Subscribe(_ =>
            {
                if (!lBoxNavigation.IsFocused && !lBoxNavigation.IsKeyboardFocusWithin)
                    return;
            }).DisposeWith(disposables);

            // Command
            this.BindCommand<MainShellView, MainShellViewModel, ReactiveCommand<Unit, Unit>, Button>(ViewModel, vm => vm.LogoutCommand, v => v.BtnLogout).DisposeWith(disposables);
        });
    }
}