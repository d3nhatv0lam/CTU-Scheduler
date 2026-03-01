using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shared.Interfaces;
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
        public void Close(object? result = null)
        {
            RequestClose?.Invoke(result);
        }

        public LogoutDialogViewModel()
        {
            AcceptCommand = ReactiveCommand.Create(() => Close(true))
                .DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.Create(() => Close(false))
                .DisposeWith(_disposables);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
