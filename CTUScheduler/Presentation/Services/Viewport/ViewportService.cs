using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.Viewport
{
    public class ViewportService : IViewportService, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly BehaviorSubject<Size> _sizeSubject = new(new Size(0, 0));
        private readonly ILogger<ViewportService> _logger;
        private IDisposable? _subscription;
        public Size CurrentSize => _sizeSubject.Value;
        public IObservable<Size> WhenSizeChanged => _sizeSubject.AsObservable();
        private bool _isDisposed;

        public ViewportService(IViewContextService viewContextService, ILogger<ViewportService> logger)
        {
            _logger = logger;
            viewContextService.WhenTopLevelChanged
                .Where(x => x is not null)
                .Select(x => x!)
                .Subscribe(Initialize)
                .DisposeWith(_disposables);

            _sizeSubject.DisposeWith(_disposables);
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

        ~ViewportService() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing)
            {
                _subscription?.Dispose();
                _disposables.Dispose();
                _logger.LogDebug("Disposed");
            }

            _isDisposed = true;
        }
    }
}