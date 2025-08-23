using System;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using DialogHostAvalonia;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.MainShell.ViewModels
{
    public class LogoutDialogViewModel: ViewModelBase, IDisposable, ICloseableDialog
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public ReactiveCommand<Unit,Unit> AcceptCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public event Action<object?>? RequestClose;
        
        public LogoutDialogViewModel()
        {
            AcceptCommand = ReactiveCommand.Create(() => RequestClose?.Invoke(true))
                .DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.Create(() => RequestClose?.Invoke(false))
                .DisposeWith(_disposables);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
