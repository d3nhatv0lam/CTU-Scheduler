using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shared.Controls.Timeline;

public class TimelineViewModel : ReactiveObject, IDisposable
{
    public ObservableCollection<TimelineNodeViewModel> Nodes { get; } = new();

    public void Dispose()
    {
        if (Nodes != null)
        {
            foreach (var node in Nodes) node.Dispose();
        }
    }
}

public enum TimelineState
{
    Completed,
    Active,
    Upcoming
}

public sealed partial class TimelineNodeViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    [Reactive] private string _title;
    [Reactive] private DateOnly _startDate;
    [Reactive] private DateOnly _endDate;
    [ObservableAsProperty] private TimelineState _state = TimelineState.Completed;
    [ObservableAsProperty] private double _progress;


    public string DateRange =>
        StartDate == EndDate
            ? StartDate.ToString("dd/MM")
            : $"{StartDate:dd/MM} → {EndDate:dd/MM}";

    public IBrush ProgressBrush => new SolidColorBrush(ProgressColor);

    private Color ProgressColor =>
        State switch
        {
            TimelineState.Upcoming =>
                Color.Parse("#CBD5E1"),

            TimelineState.Completed =>
                Color.Parse("#475569"),

            TimelineState.Active =>
                Progress switch
                {
                    < 0.5 => Lerp(
                        Color.Parse("#22C55E"),
                        Color.Parse("#EAB308"),
                        Progress / 0.5),

                    _ => Lerp(
                        Color.Parse("#EAB308"),
                        Color.Parse("#EF4444"),
                        (Progress - 0.5) / 0.5)
                },

            _ => Colors.Gray
        };

    public BoxShadows GlowStart =>
        new(new BoxShadow
        {
            Blur = 0,
            Spread = 0,
            Color = ProgressColor
        });

    public BoxShadows GlowEnd =>
        new(new BoxShadow
        {
            Blur = 0,
            Spread = 8,
            Color = WithAlpha(ProgressColor, 0)
        });


    public TimelineNodeViewModel(TimelineNode node)
    {
        Title = node.Title;
        StartDate = node.StartDate;
        EndDate = node.EndDate;

        var midNightTicker = Observable.Defer(() =>
        {
            var now = DateTime.Now;
            var due = now.Date.AddDays(1) - now;

            return Observable.Return(DateOnly.FromDateTime(DateTime.Today))
                .Concat(
                    Observable.Timer(due, TimeSpan.FromDays(1))
                        .Select(_ => DateOnly.FromDateTime(DateTime.Today)));
        });

        var pollingTicker = Observable.Interval(TimeSpan.FromMinutes(5))
            .Select(_ => DateOnly.FromDateTime(DateTime.Today));

        var dayTicker = pollingTicker.Merge(midNightTicker);

        _stateHelper = this
            .WhenAnyValue(
                x => x.StartDate, x => x.EndDate)
            .CombineLatest(dayTicker, (state, today) =>
            {
                var (startDate, endDate) = state;
                if (today > endDate) return TimelineState.Completed;
                if (today >= startDate) return TimelineState.Active;
                return TimelineState.Upcoming;
            })
            .DistinctUntilChanged()
            .ToProperty(this, nameof(State))
            .DisposeWith(_disposables);

        _progressHelper = this.WhenAnyValue(x => x.StartDate, x => x.EndDate)
            .CombineLatest(dayTicker, (range, today) =>
            {
                var total = range.Item2.DayNumber - range.Item1.DayNumber;

                if (total <= 0) return 1d;

                // đã trôi qua
                var elapsed = today.DayNumber - range.Item1.DayNumber;
                return Math.Clamp((double)elapsed / total, 0, 1);
            })
            .DistinctUntilChanged()
            .ToProperty(this, nameof(Progress))
            .DisposeWith(_disposables);

        this.WhenAnyValue(
                x => x.Progress,
                x => x.State)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ProgressBrush));
                this.RaisePropertyChanged(nameof(GlowStart));
                this.RaisePropertyChanged(nameof(GlowEnd));
            })
            .DisposeWith(_disposables);
    }

    /// Linear Interpolation 
    private static Color Lerp(Color form, Color to, double t)
    {
        t = Math.Clamp(t, 0, 1);

        return Color.FromArgb(
            (byte)(form.A + (to.A - form.A) * t),
            (byte)(form.R + (to.R - form.R) * t),
            (byte)(form.G + (to.G - form.G) * t),
            (byte)(form.B + (to.B - form.B) * t));
    }

    private static Color WithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(
            alpha,
            color.R,
            color.G,
            color.B);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}

public record TimelineNode(string Title, DateOnly StartDate, DateOnly EndDate)
{
    public bool IsPoint => StartDate == EndDate;
}