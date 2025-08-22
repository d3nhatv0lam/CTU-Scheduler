using System;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.Presentation.Base;
using DialogHostAvalonia;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.MainShell.ViewModels
{
    public class LogoutDialogViewModel: ViewModelBase, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        public string Title { get; set; } = "Đăng xuất";
        public ReactiveCommand<Unit,Unit> AcceptCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public LogoutDialogViewModel(DialogHostService.DialogIdentifier dialogIdentifier)
        {
            string _dialogIdentifier = dialogIdentifier.ToString();
            AcceptCommand = ReactiveCommand.Create(() =>
            {
                DialogHost.Close(_dialogIdentifier, true);
            }).DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.Create(() =>
            {
                DialogHost.Close(_dialogIdentifier, false);
            }).DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
