using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using DialogHostAvalonia;

namespace CTUScheduler.AppServices.Services.Dialogs
{
    public class DialogHostService: IDialogHostService
    {
        public DialogHostService() {}

        public  async Task<T?> ShowDialog<T>(object viewModel, string identifier)
        {
            try
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(viewModel, identifier));
                return result is T t? t: default;
            }
            finally
            {
                (viewModel as IDisposable)?.Dispose();
            }
        }
        
    }
}
