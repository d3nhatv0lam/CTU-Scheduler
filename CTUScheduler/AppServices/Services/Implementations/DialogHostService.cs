using Avalonia.Controls;
using Avalonia.Threading;
using CTUScheduler.AppServices.Services.Interfaces;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
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
