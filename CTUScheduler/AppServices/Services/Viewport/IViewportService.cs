using System;
using Avalonia;
using Avalonia.Controls;

namespace CTUScheduler.AppServices.Services.Viewport
{
    public interface IViewportService
    {
        Size CurrentSize { get; }
        IObservable<Size> SizeChanged { get; }
        void Initialize(Control visualRoot);
    }
}
