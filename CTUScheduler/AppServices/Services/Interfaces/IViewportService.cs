using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IViewportService
    {
        Size CurrentSize { get; }
        IObservable<Size> SizeChanged { get; }
        void Initialize(Control visualRoot);
    }
}
