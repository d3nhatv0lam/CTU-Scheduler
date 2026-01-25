using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.ViewContext;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.Viewport
{
    public class ViewportService : IViewportService, IDisposable
    {
        private readonly ILogger<ViewportService> _logger;
        private readonly BehaviorSubject<Size> _sizeSubject = new BehaviorSubject<Size>(new Size(0, 0));
        private IDisposable _subscription = null!;
        public Size CurrentSize => _sizeSubject.Value;
        public IObservable<Size> WhenSizeChanged => _sizeSubject.AsObservable();

        public ViewportService(IViewContextService viewContextService,ILogger<ViewportService> logger)
        {
            _logger = logger;
            viewContextService.WhenTopLevelChanged
                .Where(x => x is not null)
                .Select(x => x!)
                .Subscribe(Initialize);
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
            _logger.LogInformation("ViewportService disposed");
        }
    }
}
