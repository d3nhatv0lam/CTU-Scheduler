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
    public ObservableCollection<TimelineNodeViewModel> Nodes { get; } = new();

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
    [ObservableAsProperty] private TimelineState _state = TimelineState.Completed;
    [ObservableAsProperty] private double _progress;

    private static string FormatDateTime(DateTime dt, string timeOnlyFormat = "HH:mm dd/MM",
        string dateOnlyFormat = "dd/MM")
    {
        return dt.TimeOfDay == TimeSpan.Zero
            ? dt.ToString(dateOnlyFormat)
            : dt.ToString(timeOnlyFormat);
    }

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

    // --- CÁC THUỘC TÍNH PHỤC VỤ GIAO DIỆN PREMIUM (VISUAL CARDS) ---

    public BoxShadows CardBoxShadow => State switch
    {
        TimelineState.Active => new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 6,
            Blur = 20,
            Spread = 0,
            Color = WithAlpha(ProgressColor, 0x1A) // Dynamic soft glow matching remaining time color
        }),
        TimelineState.Completed => new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 2,
            Blur = 6,
            Spread = 0,
            Color = Color.Parse("#080F172A") // Subtle elevation shadow for completed
        }),
        _ => new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = 0,
            Spread = 0,
            Color = Colors.Transparent
        })
    };

    public IBrush TitleColor => State switch
    {
        TimelineState.Active => new SolidColorBrush(Color.Parse("#1E1B4B")), // Indigo-950 (extremely sharp & premium)
        TimelineState.Completed => new SolidColorBrush(Color.Parse("#334155")), // Slate-700
        TimelineState.Upcoming => new SolidColorBrush(Color.Parse("#94A3B8")), // Slate-400 (slightly dimmed)
        _ => Brushes.Black
    };

    public IBrush DateColor => State switch
    {
        TimelineState.Active => new SolidColorBrush(Color.Parse("#4F46E5")), // Indigo-600 accent
        TimelineState.Completed => new SolidColorBrush(Color.Parse("#64748B")), // Slate-500
        TimelineState.Upcoming => new SolidColorBrush(Color.Parse("#CBD5E1")), // Slate-300
        _ => Brushes.Gray
    };

    public FontWeight TitleFontWeight => State switch
    {
        TimelineState.Active => FontWeight.Bold,
        _ => FontWeight.SemiBold
    };

    public IBrush CardBackground => State switch
    {
        TimelineState.Active => new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(WithAlpha(ProgressColor, 0x0A), 0.0), // Dynamic matching very soft tint (approx 4%)
                new GradientStop(Color.Parse("#FFFFFF"), 1.0) // Blending into pure white
            }
        },
        TimelineState.Completed => new SolidColorBrush(Color.Parse("#F8FAFC")), // Slate-50
        TimelineState.Upcoming => new SolidColorBrush(Color.Parse("#FFFFFF")), // Clean white
        _ => Brushes.Transparent
    };

    public IBrush CardBorderBrush => State switch
    {
        TimelineState.Active => new SolidColorBrush(WithAlpha(ProgressColor,
            0x4D)), // Dynamic matching border (30% opacity)
        TimelineState.Completed => new SolidColorBrush(Color.Parse("#E2E8F0")), // Slate-200
        TimelineState.Upcoming => new SolidColorBrush(Color.Parse("#F1F5F9")), // Slate-100 (clean & soft outline)
        _ => Brushes.Transparent
    };

    public TimelineNodeViewModel(TimelineNode node)
    {
        Title = node.Title;
        StartDate = node.StartDate;
        EndDate = node.EndDate;
        Subtitle = node.Subtitle;
        NodeType = node.Type;

        // Ticker thời gian thực cập nhật mỗi 1 phút để đếm ngược chính xác, bắt buộc chạy trên Main UI Thread
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
                this.RaisePropertyChanged(nameof(TitleColor));
                this.RaisePropertyChanged(nameof(DateColor));
                this.RaisePropertyChanged(nameof(TitleFontWeight));
                this.RaisePropertyChanged(nameof(CardBackground));
                this.RaisePropertyChanged(nameof(CardBorderBrush));
                this.RaisePropertyChanged(nameof(CardBoxShadow));
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