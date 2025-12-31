using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;

namespace CTUScheduler.AppServices.Models;

public class RuntimeCourse : IDisposable, INotifyPropertyChanged
{
    private readonly SourceCache<CourseSection, string> _sectionsCache;
    // groupCode -> hashset<GUID as string>
    private readonly ConcurrentDictionary<string, HashSet<string>> _sectionOwners = new();
    private string _name_VN = string.Empty;
    private int _credits = 0;
    private int _theorySessions = 0;
    private int _practicalSessions = 0;
    
    public string Code { get; }
    public string Name_VN
    {
        get => _name_VN;
        private set => SetProperty(ref _name_VN, value);
    }

    public int Credits
    {
        get => _credits;
        private set => SetProperty(ref _credits, value);
    }

    public int TheorySessions
    {
        get => _theorySessions;
        private set => SetProperty(ref _theorySessions, value);
    }

    public int PracticalSessions
    {
        get => _practicalSessions; 
        private set => SetProperty(ref _practicalSessions, value);
    }
    public IObservableCache<CourseSection, string> Sections => _sectionsCache.AsObservableCache();
    public IEnumerable<string> ActiveGroups => _sectionsCache.Keys;
    
    public RuntimeCourse(Course dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        
        Code = dto.Code;
        Name_VN = dto.Name_VN;
        Credits = dto.Credits;
        TheorySessions = dto.TheorySessions;
        PracticalSessions = dto.PracticalSessions;
        _sectionsCache = new SourceCache<CourseSection, string>(x => x.Group);
    }

    /// <summary>
    /// Đăng ký nhóm cho 1 profile
    /// </summary>
    /// <param name="section"></param>
    /// <param name="ownerId"></param>
    /// <returns>True nếu nội dung section thay đổi hoặc là đăng ký mới.</returns>
    public bool RegisterSection(CourseSection section, string ownerId)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
        
        var owners = _sectionOwners.GetOrAdd(section.Group, _ => new HashSet<string>());
        lock (owners)
        {
            owners.Add(ownerId);
        }
        
        _sectionsCache.AddOrUpdate(section);
        return true;
    }

    /// <summary>
    ///  Hủy đăng ký 1 section của profile cụ thể
    /// </summary>
    /// <param name="groupCode"></param>
    /// <param name="ownerId"></param>
    /// <returns>Return true if the RuntimeCourse has no sections.</returns>
    public bool UnregisterSection(string groupCode, string ownerId)
    {
        if (string.IsNullOrEmpty(groupCode)) throw new ArgumentNullException(nameof(groupCode));
        if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
        
        if (!_sectionOwners.TryGetValue(groupCode, out var owners))
        {
            if (_sectionsCache.Lookup(groupCode).HasValue)
                _sectionsCache.Remove(groupCode);
            return _sectionsCache.Count == 0;
        }

        bool isGroupEmpty;
        lock (owners)
        {
            owners.Remove(ownerId);
            isGroupEmpty = owners.Count == 0;
        }

        if (isGroupEmpty)
        {
            _sectionOwners.TryRemove(groupCode, out _);
            _sectionsCache.Remove(groupCode);
        }
        return _sectionsCache.Count == 0;
    }
    
    public bool IsSectionRegistered(string groupCode) => _sectionOwners.ContainsKey(groupCode);

    /// <summary>
    /// Synchronously merge data from Api into local data.
    /// </summary>
    /// <param name="course"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void Merge(Course course)
    {
        ArgumentNullException.ThrowIfNull(course);
        if (!string.Equals(course.Code, this.Code, StringComparison.OrdinalIgnoreCase)) 
            throw new ArgumentException("Course code mismatch", nameof(course));
        
        this.Credits = course.Credits;
        this.Name_VN = course.Name_VN;
        this.TheorySessions = course.TheorySessions;
        this.PracticalSessions = course.PracticalSessions;
        
        var serverSectionDict = course.Sections.ToDictionary(s => s.Group);
        _sectionsCache.Edit(innerList =>
        {
            var allLocalSections = innerList.Items.ToList();
            foreach (var localSection in allLocalSections)
            {
                if (serverSectionDict.TryGetValue(localSection.Group, out var serverSection))
                {
                   innerList.AddOrUpdate(serverSection);
                }
                else if (!localSection.IsCancelled)
                {
                    localSection.IsCancelled = true;
                    innerList.AddOrUpdate(localSection);
                }
            }
        });
    }
    public Course ToCourse() => new Course()
    {
        Code = this.Code,
        Name_VN = this.Name_VN,
        Credits = this.Credits,
        TheorySessions = this.TheorySessions,
        PracticalSessions = this.PracticalSessions,
        Sections = this._sectionsCache.Items.ToList()
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    public void Dispose() => _sectionsCache.Dispose();
}