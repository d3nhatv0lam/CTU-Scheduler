using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Base;
using Irihi.Avalonia.Shared.Contracts;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shared.Dialogs.ViewModels
{
    public class ConfirmDialogViewModel : ViewModelBase, IDialogContext, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private string _title = "";
        private string _message = "";
        private string _confirmText = "";
        private string _cancelText = "";
        private bool _isDestructive = false;


        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public string ConfirmText
        {
            get => _confirmText;
            set => this.RaiseAndSetIfChanged(ref _confirmText, value);
        }

        public string CancelText
        {
            get => _cancelText;
            set => this.RaiseAndSetIfChanged(ref _cancelText, value);
        }

        public bool IsDestructive
        {
            get => _isDestructive;
            set => this.RaiseAndSetIfChanged(ref _isDestructive, value);
        }

        public ReactiveCommand<Unit, Unit> AcceptCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }


        private EventHandler<object?>? _ursaRequestClose;

        public event EventHandler<object?>? RequestClose
        {
            add => _ursaRequestClose += value;
            remove => _ursaRequestClose -= value;
        }


        public ConfirmDialogViewModel()
        {
            AcceptCommand = ReactiveCommand.Create(() => { Close(true); })
                .DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.Create(() => { Close(false); })
                .DisposeWith(_disposables);
        }

        void IDialogContext.Close() => _ursaRequestClose?.Invoke(this, null);

        public void Close(object? result = null) => _ursaRequestClose?.Invoke(this, result);


        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}