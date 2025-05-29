using Avalonia;
using Avalonia.Controls;
using CTUScheduler.AppServices.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class ViewportService : IViewportService, IDisposable
    {
        private readonly ILogger<ViewportService> _logger;
        private readonly BehaviorSubject<Size> _sizeSubject = new BehaviorSubject<Size>(new Size(0, 0));
        private IDisposable _subscription = null!;
        public Size CurrentSize => _sizeSubject.Value;
        public IObservable<Size> SizeChanged => _sizeSubject.AsObservable();

        public ViewportService(ILogger<ViewportService> logger)
        {
            _logger = logger;
        }

        public void Initialize(Control visualRoot)
        {
            if (visualRoot == null)
                throw new ArgumentException("Visual root must be a (Control or Window)", nameof(visualRoot));

            _subscription?.Dispose();

            _subscription = visualRoot.GetObservable(Visual.BoundsProperty)
                .Select(bounds => new Size(bounds.Width, bounds.Height))
                .DistinctUntilChanged()
                .Subscribe(size => _sizeSubject.OnNext(size));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _sizeSubject.Dispose();
        }
    }
}
