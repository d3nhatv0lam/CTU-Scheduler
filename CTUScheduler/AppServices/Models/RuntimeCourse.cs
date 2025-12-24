using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;

namespace CTUScheduler.AppServices.Models;

public class RuntimeCourse : IDisposable
{
    // Ref count
    private readonly ConcurrentDictionary<string, int> _groupRefCounts = new();
    private readonly SourceCache<CourseSection, string> _sectionsCache;
    public string Code { get; }
    public string Name_VN { get; }
    public int Credits { get; }
    public int TheorySessions { get;}
    public int PracticalSessions { get; }

    public IObservableCache<CourseSection, string> Sections => _sectionsCache.AsObservableCache();
    
    public IEnumerable<string> ActiveGroups => _sectionsCache.Keys;
    
    public RuntimeCourse(Course dto)
    {
        Code = dto.Code;
        Name_VN = dto.Name_VN;
        Credits = dto.Credits;
        TheorySessions = dto.TheorySessions;
        PracticalSessions = dto.PracticalSessions;
        _sectionsCache = new SourceCache<CourseSection, string>(x => x.Group);
    }
    
    public void RegisterSection(CourseSection section)
    {
        _groupRefCounts.AddOrUpdate(section.Group, 1, (_, currentCount) => currentCount + 1);
        _sectionsCache.AddOrUpdate(section);
    }

    public bool UnregisterSection(string groupCode)
    {
        if (!_groupRefCounts.ContainsKey(groupCode))
        {
            if (_sectionsCache.Lookup(groupCode).HasValue) 
                _sectionsCache.Remove(groupCode);
            return _sectionsCache.Count == 0;
        }

        int newCount = _groupRefCounts.AddOrUpdate(groupCode, 0, (_, old) => old - 1);

        if (newCount <= 0)
        {
            _groupRefCounts.TryRemove(groupCode, out _);
            _sectionsCache.Remove(groupCode);
        }

        return _sectionsCache.Count == 0;
    }

    public void UpdateSection(CourseSection section)
    {
        if (_groupRefCounts.ContainsKey(section.Group))
        {
            _sectionsCache.AddOrUpdate(section);
        }
    }
    
    public void UpdateSections(IEnumerable<CourseSection> sections)
    {
        var validSections = sections
            .Where(s => _groupRefCounts.ContainsKey(s.Group))
            .ToList();
        
        if (validSections.Count > 0)
        {
            _sectionsCache.AddOrUpdate(validSections);
        }
    }
    
    /// <summary>
    /// Cập nhật data và phát hiện các nhóm đã bị xóa trên server.
    /// </summary>
    /// <returns>Danh sách các mã nhóm (Group Code) có trong Local nhưng không tìm thấy trong data mới.</returns>
    public List<string> UpdateSectionsAndDetectMissing(IEnumerable<CourseSection> newSectionsFromApi)
    {
        var newSections = newSectionsFromApi.ToList();
        // bảng tra cứu
        var serverGroups = newSections.Select(s => s.Group).ToHashSet();
        
        var userActiveGroups = _groupRefCounts.Keys.ToList();
        
        var vanishedGroups = userActiveGroups
            .Where(localGroup => !serverGroups.Contains(localGroup))
            .ToList();
        
        var validSectionsToUpdate = newSections
            .Where(s => _groupRefCounts.ContainsKey(s.Group))
            .ToList();

        if (validSectionsToUpdate.Count > 0)
        {
            _sectionsCache.AddOrUpdate(validSectionsToUpdate);
        }
        
        return vanishedGroups;
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

    public void Dispose() => _sectionsCache.Dispose();
}