using CTUScheduler.Presentation.ViewModels.Base;
using DialogHostAvalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.Shells.Components
{
    public class LogoutDialogViewModel: ViewModelBase, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        private string _dialogIdentifier;
        public string Title { get; set; } = "Đăng xuất";
        public ReactiveCommand<Unit,Unit> AcceptCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public LogoutDialogViewModel(string dialogIdentifer)
        {
            _dialogIdentifier = dialogIdentifer;
            AcceptCommand = ReactiveCommand.Create(() =>
            {
                DialogHost.Close(_dialogIdentifier, true);
                Dispose();
            }).DisposeWith(_disposables);
            CancelCommand = ReactiveCommand.Create(() =>
            {
                DialogHost.Close(_dialogIdentifier, false);
                Dispose();
            }).DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
