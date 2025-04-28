using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.Shells;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CTUScheduler.Presentation.Views.Shells;

public partial class MainLayoutView : ReactiveUserControl<MainLayoutViewModel>
{
    public MainLayoutView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            
            this.OneWayBind(ViewModel, vm => vm.NavigationItems, v => v.lBoxNavigation.ItemsSource).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Title, v => v.txtTitle.Text).DisposeWith(disposables);

            this.WhenAnyValue(x => x.lBoxNavigation.SelectedItem).WhereNotNull().BindTo(ViewModel, vm => vm.SelectedItem).DisposeWith(disposables);

            Observable.FromEventPattern<EventArgs>(lBoxNavigation, nameof(lBoxNavigation.Loaded)).Subscribe(_ =>
            {
                if (lBoxNavigation.Items.Count > 0)
                    lBoxNavigation.SelectedIndex = 0;

            }).DisposeWith(disposables);

            


            Observable.FromEventPattern<EventArgs>(lBoxNavigation, nameof(lBoxNavigation.PointerReleased)).Subscribe(_ =>
            {
                if (!lBoxNavigation.IsFocused && !lBoxNavigation.IsKeyboardFocusWithin)
                    return;
                LeftDrawer.OptionalCloseLeftDrawer();
            }).DisposeWith(disposables);

            // Command
            this.BindCommand(ViewModel, vm => vm.LogoutCommand, v => v.BtnLogout).DisposeWith(disposables);
        });
    }
}