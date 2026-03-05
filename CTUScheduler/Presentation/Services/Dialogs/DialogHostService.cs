using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using DialogHostAvalonia;

namespace CTUScheduler.Presentation.Services.Dialogs
{
    public partial class DialogHostService: IDialogHostService
    {
        public async Task<TResult?> ShowDialogAsync<TViewModel, TResult>(
            TViewModel viewModel, 
            DialogIdentifier identifier, 
            bool isDisposeViewModel = true)
            where TViewModel: class
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
                
                return result is TResult t? t: default;
            }
            finally
            {
                if (isDisposeViewModel)
                    (viewModel as IDisposable)?.Dispose();
            }
        }
    }
}
