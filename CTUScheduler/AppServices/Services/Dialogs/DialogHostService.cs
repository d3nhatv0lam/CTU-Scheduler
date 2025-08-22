using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using DialogHostAvalonia;

namespace CTUScheduler.AppServices.Services.Dialogs
{
    public class DialogHostService: IDialogHostService
    {
        public enum DialogIdentifier
        {
            MainLayout,
            Timetable,
        }
        public DialogHostService() {}

        public  async Task<T?> ShowDialog<T>(object viewModel, DialogIdentifier identifier)
        {
            try
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(viewModel, identifier.ToString()));
                return result is T t? t: default;
            }
            finally
            {
                (viewModel as IDisposable)?.Dispose();
            }
        }
        
    }
}
