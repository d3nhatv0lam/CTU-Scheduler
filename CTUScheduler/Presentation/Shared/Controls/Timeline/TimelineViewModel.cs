using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using CTUScheduler.Core.Models.TeachingPlan;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shared.Controls.Timeline;

public class TimelineViewModel : ReactiveObject, IViewModel, IDisposable
{
    public ObservableCollection<TimelineNodeViewModel> Nodes { get; } = [];
    

    // design test
    // public TimelineViewModel()
    // {
    //     var node = new TimelineNode(
    //         Title:"test",
    //         StartDate:DateTime.Now,
    //         EndDate:DateTime.Now.AddDays(10),
    //         TimelineNodeType.Range);
    //     
    //     Nodes.Add(new TimelineNodeViewModel(node));
    //     Nodes.Add(new TimelineNodeViewModel(node));
    // }

    public void Dispose()
    {
        foreach (var node in Nodes) node.Dispose();
    }
}

public enum TimelineState
{
    Completed,
    Active,
    Upcoming
}

public partial class TimelineNodeViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    [Reactive] private string _title;
    [Reactive] private DateTime _startDate;
    [Reactive] private DateTime _endDate;
    [Reactive] private string? _subtitle;
    [Reactive] private TimelineNodeType _nodeType;
    [Reactive] private TeachingPlanStep _step;
    [ObservableAsProperty] private TimelineState _state = TimelineState.Completed;
    [ObservableAsProperty] private double _progress;

    public string DateRange => NodeType switch
    {
        TimelineNodeType.SinglePoint => FormatDateTime(StartDate),
        TimelineNodeType.DeadlineOrEnd => $"Hạn cuối: {FormatDateTime(EndDate)}",
        TimelineNodeType.StartFrom => $"Bắt đầu từ: {FormatDateTime(StartDate)}",
        TimelineNodeType.Range => $"{FormatDateTime(StartDate)} → {FormatDateTime(EndDate)}",
        _ => StartDate == EndDate
            ? FormatDateTime(StartDate)
            : $"{FormatDateTime(StartDate)} → {FormatDateTime(EndDate)}"
    };

    public IBrush ProgressBrush => new SolidColorBrush(ProgressColor);

    private Color ProgressColor =>
        State switch
        {
            TimelineState.Upcoming =>
                Color.Parse("#CBD5E1"), // Muted slate-300

            TimelineState.Completed =>
                Color.Parse("#475569"), // Dark slate-600 as original!

            TimelineState.Active =>
                Progress switch
                {
                    < 0.5 => Lerp(
                        Color.Parse("#10B981"), // Green start
                        Color.Parse("#F59E0B"), // Yellow mid
                        Progress / 0.5),

                    _ => Lerp(
                        Color.Parse("#F59E0B"), // Yellow mid
                        Color.Parse("#EF4444"), // Red endW
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


    public BoxShadows? CardBoxShadow => State == TimelineState.Active
        ? new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 6,
            Blur = 20,
            Spread = 0,
            Color = WithAlpha(ProgressColor, 0x1A) // Dynamic soft glow matching remaining time color
        })
        : null;

    public IBrush? CardBackground => State == TimelineState.Active
        ? new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(WithAlpha(ProgressColor, 0x0A), 0.0), // Dynamic matching very soft tint (approx 4%)
                new GradientStop(Color.Parse("#FFFFFF"), 1.0) // Blending into pure white
            }
        }
        : null;

    public IBrush? CardBorderBrush => State == TimelineState.Active
        ? new SolidColorBrush(WithAlpha(ProgressColor, 0x4D)) // Dynamic matching border (30% opacity)
        : null;

    public TimelineNodeViewModel(TimelineNode node)
    {
        Title = node.Title;
        StartDate = node.StartDate;
        EndDate = node.EndDate;
        Subtitle = node.Subtitle;
        NodeType = node.Type;
        Step = node.GetStepType();

        // Ticker thời gian thực cập nhật mỗi 1 phút để đếm ngược chính xác
        var timeTicker = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1), RxSchedulers.MainThreadScheduler)
            .Select(_ => DateTime.Now);

        this.WhenAnyValue(
                x => x.StartDate,
                x => x.EndDate,
                x => x.NodeType)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(DateRange)))
            .DisposeWith(_disposables);

        _stateHelper = this
            .WhenAnyValue(
                x => x.StartDate, x => x.EndDate)
            .CombineLatest(timeTicker, (state, now) =>
            {
                var (startDate, endDate) = state;
                if (now > endDate) return TimelineState.Completed;
                if (now >= startDate) return TimelineState.Active;
                return TimelineState.Upcoming;
            })
            .DistinctUntilChanged()
            .ToProperty(this, nameof(State), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(_disposables);

        _progressHelper = this.WhenAnyValue(x => x.StartDate, x => x.EndDate)
            .CombineLatest(timeTicker, (range, now) =>
            {
                var totalSeconds = (range.Item2 - range.Item1).TotalSeconds;

                if (totalSeconds <= 0) return 1d;

                // Tỷ lệ phần trăm thời gian trôi qua thực tế
                var elapsedSeconds = (now - range.Item1).TotalSeconds;
                return Math.Clamp(elapsedSeconds / totalSeconds, 0, 1);
            })
            .DistinctUntilChanged()
            .ToProperty(this, nameof(Progress), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(_disposables);

        this.WhenAnyValue(
                x => x.Progress,
                x => x.State)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ProgressBrush));
                this.RaisePropertyChanged(nameof(GlowStart));
                this.RaisePropertyChanged(nameof(GlowEnd));
                this.RaisePropertyChanged(nameof(CardBackground));
                this.RaisePropertyChanged(nameof(CardBorderBrush));
                this.RaisePropertyChanged(nameof(CardBoxShadow));
            })
            .DisposeWith(_disposables);
    }

    private static string FormatDateTime(DateTime dt, string timeOnlyFormat = "HH:mm dd/MM",
        string dateOnlyFormat = "dd/MM")
    {
        return dt.TimeOfDay == TimeSpan.Zero
            ? dt.ToString(dateOnlyFormat)
            : dt.ToString(timeOnlyFormat);
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