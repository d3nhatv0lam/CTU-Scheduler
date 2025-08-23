using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CTUScheduler.Core.Interfaces;
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

        public async Task<T?> ShowDialogAsync<T>(object viewModel, DialogIdentifier identifier)
        {
            try
            {
                var identifierString = identifier.ToString();
                Action<object?>? handler = null;
                
                if (viewModel is ICloseableDialog closeableDialog)
                {
                    handler = (result) => DialogHost.Close(identifierString, result);
                    closeableDialog.RequestClose += handler;
                }
                
                var result = await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(viewModel, identifierString));
                
                if (viewModel is ICloseableDialog closeableDialog2 && handler != null)
                {
                    closeableDialog2.RequestClose -= handler;
                }
                
                return result is T t? t: default;
            }
            finally
            {
                (viewModel as IDisposable)?.Dispose();
            }
        }
    }
}
