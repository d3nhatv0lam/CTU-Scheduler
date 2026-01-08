using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Media;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Models;

public class ScheduleGroupCellShared: ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    private readonly ObservableAsPropertyHelper<string> _courseCode;
    private readonly ObservableAsPropertyHelper<string> _courseNameVn;
    private readonly ObservableAsPropertyHelper<string> _group;
    private readonly ObservableAsPropertyHelper<string> _lecturer;
    private readonly ObservableAsPropertyHelper<int> _credit;
    private readonly ObservableAsPropertyHelper<string> _remainingConcatTotalStudents;
    
    private readonly ObservableAsPropertyHelper<bool> _isHighStatus;
    private readonly ObservableAsPropertyHelper<bool> _isMediumStatus;
    private readonly ObservableAsPropertyHelper<bool> _isLowStatus;
    private readonly ObservableAsPropertyHelper<bool> _isArchivedStatus;

    public IBrush BackgroundColor { get; }
    
    public ScheduleGroupCellShared(ICourseDisplaySource source, IBrush color)
    {
        BackgroundColor = color;

        _courseCode = source.Code.ToProperty(this, x => x.CourseCode).DisposeWith(_disposables);
        _courseNameVn = source.Name.ToProperty(this, x => x.CourseName_VN).DisposeWith(_disposables);
        _group = source.Group.ToProperty(this, x => x.Group).DisposeWith(_disposables);
        _lecturer = source.Lecturer.ToProperty(this, x => x.Lecturer).DisposeWith(_disposables);
        _credit = source.Credits.ToProperty(this, x => x.Credits).DisposeWith(_disposables);
        
        var statusStream = source.RemainingStudents.CombineLatest(source.TotalStudents, 
            (rem, total) => new { rem, total })
            .Replay(1).RefCount();

        _remainingConcatTotalStudents = statusStream
            .Select(x => $"Sĩ số: {x.rem}/{x.total}")
            .ToProperty(this, x => x.RemainingConcatTotalStudents)
            .DisposeWith(_disposables);

        var levelStream = statusStream
            .Select(x => CalculateStatus(x.rem, x.total))
            .Replay(1)
            .RefCount();

        _isHighStatus = levelStream.Select(l => l == RemainingLevel.High).ToProperty(this, x => x.IsHighStatus).DisposeWith(_disposables);
        _isMediumStatus = levelStream.Select(l => l == RemainingLevel.Medium).ToProperty(this, x => x.IsMediumStatus).DisposeWith(_disposables);
        _isLowStatus = levelStream.Select(l => l == RemainingLevel.Low).ToProperty(this, x => x.IsLowStatus).DisposeWith(_disposables);
        _isArchivedStatus = levelStream.Select(l => l == RemainingLevel.Archived).ToProperty(this, x => x.IsArchivedStatus).DisposeWith(_disposables);
    }
    
    public string CourseCode => _courseCode.Value;
    public string CourseName_VN => _courseNameVn.Value;
    public string Group => _group.Value;
    public string Lecturer => _lecturer.Value;
    public int Credits => _credit.Value;
    public string RemainingConcatTotalStudents => _remainingConcatTotalStudents.Value;
    
    public string NameConcat => $"{CourseCode}-{Group}-{CourseName_VN}";
    
    public bool IsHighStatus => _isHighStatus.Value;
    public bool IsMediumStatus => _isMediumStatus.Value;
    public bool IsLowStatus => _isLowStatus.Value;
    public bool IsArchivedStatus => _isArchivedStatus.Value;

    public void Dispose() => _disposables.Dispose();

    private static RemainingLevel CalculateStatus(int remaining, int total)
    {
        if (total <= 0  || remaining < 0) return RemainingLevel.Archived;
        if (remaining == 0) return RemainingLevel.None;
        double ratio = (double)remaining / total;
        if (ratio < 0.1) return RemainingLevel.Low;
        if (ratio <= 0.4) return RemainingLevel.Medium;
        return RemainingLevel.High;
    }
}
