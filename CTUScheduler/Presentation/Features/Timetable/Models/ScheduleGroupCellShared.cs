using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.Models;

public class ScheduleGroupCellShared: ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new ();
    private readonly ObservableAsPropertyHelper<string> _remainingConcatTotalStudents;
    private int _remainingStudents;
    private int _totalStudents;
    private bool _isHighStatus;
    private bool _isMediumStatus;
    private bool _isLowStatus;
    private bool _isArchivedStatus;
    
    public enum RemainingLevel
    {
        Archived, // not active
        None, // 0%
        Low, // Dưới 10%
        Medium, // 10–40%
        High // Trên 40%
    }
    
    public IBrush BackgroundColor { get; set; } = Brushes.Transparent;
    public string CourseCode { get; set; } = string.Empty;
    // ReSharper disable once InconsistentNaming
    public string CourseName_VN { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Lecturer { get; set; } = string.Empty;
    public int Credit { get; set; }

    public int RemainingStudents
    {
        get => _remainingStudents;
        set => this.RaiseAndSetIfChanged(ref _remainingStudents, value);
    }
        
    public int TotalStudents
    {
        get => _totalStudents;
        set => this.RaiseAndSetIfChanged(ref _totalStudents, value);
    }

    public bool IsArchivedStatus
    {
        get => _isArchivedStatus;
        set => this.RaiseAndSetIfChanged(ref _isArchivedStatus, value);
    }
    public bool IsLowStatus
    {
        get => _isLowStatus;
        set => this.RaiseAndSetIfChanged(ref _isLowStatus, value);
    }

    public bool IsMediumStatus
    {
        get => _isMediumStatus;
        set => this.RaiseAndSetIfChanged(ref _isMediumStatus, value);
    }

    public bool IsHighStatus
    {
        get => _isHighStatus;
        set => this.RaiseAndSetIfChanged(ref _isHighStatus, value);
    }
    
    public string RemainingConcatTotalStudents => _remainingConcatTotalStudents.Value;
    
    private RemainingLevel RemainingStatus
    {
        get
        {
            if (TotalStudents <= 0  || RemainingStudents < 0) return RemainingLevel.Archived;
            double ratio = (double)RemainingStudents / TotalStudents;
            if (ratio == 0) return RemainingLevel.None;
            if (ratio < 0.1) return RemainingLevel.Low;
            if (ratio <= 0.4) return RemainingLevel.Medium;
            return RemainingLevel.High;
        }
    }
    
    public ScheduleGroupCellShared()
    {
        _remainingConcatTotalStudents= this.WhenAnyValue(x => x.RemainingStudents, x => x.TotalStudents)
            .Select(tuple =>
            {
                var (remaining, total) = tuple;
                return $"Sĩ số: {remaining}/{total}";
            })
            .ToProperty(this, nameof(RemainingConcatTotalStudents), scheduler:RxApp.MainThreadScheduler)
            .DisposeWith(_disposables);

        // Nếu cần update status khác khi property thay đổi
        this.WhenAnyValue(x => x.RemainingConcatTotalStudents)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => OnUpdateRemainingStudentsStatus())
            .DisposeWith(_disposables);
    }
    
    private void OnUpdateRemainingStudentsStatus()
    {
        var status = RemainingStatus;
        IsArchivedStatus = status == RemainingLevel.Archived; 
        IsLowStatus = status == RemainingLevel.Low;
        IsMediumStatus = status == RemainingLevel.Medium;
        IsHighStatus = status == RemainingLevel.High;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}