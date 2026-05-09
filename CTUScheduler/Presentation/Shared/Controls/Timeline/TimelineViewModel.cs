using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shared.Controls.Timeline;

public class TimelineViewModel : ReactiveObject
{
    public ObservableCollection<TimelineNodeViewModel> Nodes { get; }
    
    public TimelineViewModel()
    {
        List<TimelineNode> nodes =
        [
            new TimelineNode("Công bố thời khóa biểu",
                new(2026, 3, 30),
                new(2026, 4, 5)),

            new TimelineNode("Đăng ký học phần đợt 1",
                new(2026, 4, 6),
                new(2026, 4, 19)),

            new TimelineNode("Điều chỉnh KHHT",
                new(2026, 4, 11),
                new(2026, 4, 19)),

            new TimelineNode("Hạn cuối mở lớp",
                new(2026, 4, 17),
                new(2026, 4, 17)),

            new TimelineNode("Xử lý số liệu",
                new(2026, 4, 20),
                new(2026, 5, 13)),

            new TimelineNode("Học kỳ 3 diễn ra",
                new(2026, 5, 11),
                new(2026, 8, 23)),

            new TimelineNode("Điều chỉnh đợt 2",
                new(2026, 5, 11),
                new(2026, 5, 17)),

            new TimelineNode("Mở lại KHHT",
                new(2026, 5, 14),
                new(2026, 5, 14))
        ];

        Nodes = new ObservableCollection<TimelineNodeViewModel>(
            nodes
                .OrderBy(x => x.StartDate)
                .ThenBy(x => x.EndDate)
                .Select(x => new TimelineNodeViewModel(x)));
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
    [Reactive] private string _title;
    [Reactive] private DateTime _startDate;
    [Reactive] private DateTime _endDate;
    [ObservableAsProperty] private TimelineState _state = TimelineState.Completed;


    public string DateRange =>
        StartDate == EndDate
            ? StartDate.ToString("dd/MM")
            : $"{StartDate:dd/MM} → {EndDate:dd/MM}";

    public TimelineNodeViewModel(TimelineNode node)
    {
        Title = node.Title;
        StartDate = node.StartDate;
        EndDate = node.EndDate;

        var midNightTicker = Observable.Defer(() =>
        {
            var now = DateTime.Now;
            var nextDay = now.Date.AddDays(1);
            var due = nextDay - now;

            return Observable.Return(DateTime.Today)
                .Concat(
                    Observable.Timer(due, TimeSpan.FromDays(1))
                        .Select(_ => DateTime.Today));
        });

        var heartBeat = Observable.Interval(TimeSpan.FromMinutes(5))
            .Select(_ => DateTime.Today);

        var dayTicker = heartBeat.Merge(midNightTicker);

        _stateHelper = this.WhenAnyValue<TimelineNodeViewModel, (DateTime startDate, DateTime endDate), DateTime, DateTime>(x => x.StartDate, x => x.EndDate,
                (startDate, endDate) => (startDate, endDate))
            .CombineLatest(dayTicker, (state, today) =>
            {
                if (today > state.endDate) return TimelineState.Completed;
                if (today >= state.startDate) return TimelineState.Active;
                return TimelineState.Upcoming;
            })
            .DistinctUntilChanged()
            .ToProperty(this, nameof(State));
    }

    public void Dispose()
    {
        _stateHelper.Dispose();
    }
}

public record TimelineNode(string Title, DateTime StartDate, DateTime EndDate)
{
    public bool IsPoint => StartDate == EndDate;
}