using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.Adapter;

public class TimetableLayoutVmAdapter: ITimetableLayoutAdapter
{
    private readonly ICourseScheduleService _scheduleService;
    private readonly ILogger<TimetableLayoutVmAdapter> _logger;
    private readonly Dictionary<ScheduleTable,TimetableLayoutViewModel> _vmDict = new();

    private readonly Action<TimetableLayoutViewModel> _requestUpdateHandler;
    private readonly Action<TimetableLayoutViewModel> _requestBuildTimetableHandler;
    
    public TimetableLayoutVmAdapter(ICourseScheduleService scheduleService, ILogger<TimetableLayoutVmAdapter> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
        
        _requestBuildTimetableHandler = vm =>
        {
            var list = vm.GetScheduleData();
            var timetableBuildData = list.Select(x =>
                {
                    var (code, group) = x;
                    return _scheduleService.GetSectionChoice(code, group);
                })
                .OfType<SectionChoice>()
                .ToList();
            vm.ApplyBuildTimetableData(timetableBuildData);
        };
        
        _requestUpdateHandler =  vm =>
        {
            var list = vm.GetScheduleData();
            var updatedSections = list.Select(x =>
            {
                var (code,group) = x;
                return _scheduleService.GetCourseSection(code, group);
            })
            .OfType<CourseSection>()
            .ToList();
            vm.ApplyUpdatedTimetableData(updatedSections);
        };
    }
    
    public TimetableLayoutViewModel GetOrCreateLayout(ScheduleTable table)
    {
        if (_vmDict.TryGetValue(table, out var value))
            return value;
        
        var vm = new TimetableLayoutViewModel(table);
        Register(vm);
        var disposable = Disposable.Create(() =>
        {
            Unregister(vm);
        });
        vm.AddDisposable(disposable);
        return vm;
    }
    
    public async Task UpdateAsync()
    {
        if (!await _scheduleService.TryReloadCourseDataAsync()) return;
        foreach (var vm in _vmDict.Values)
        {
            vm.Update();
        }
    }

    public void Register(TimetableLayoutViewModel viewModel)
    {
        if (viewModel is null) return;
        try
        {
            _vmDict.Add(viewModel.ToModel(), viewModel);
            viewModel.UpdateRequested += _requestUpdateHandler;
            viewModel.BuildTimetableRequested += _requestBuildTimetableHandler;
            viewModel.BuildTimetable();
        }
        catch (Exception ex)
        {
            viewModel.UpdateRequested -= _requestUpdateHandler;
            viewModel.BuildTimetableRequested -= _requestBuildTimetableHandler;
            _logger.LogError(ex, "Failed to register timetable layout view model");
        }
    }

    public void Unregister(TimetableLayoutViewModel viewModel)
    {
        if (viewModel is null) return;
        try
        {
            _vmDict.Remove(viewModel.ToModel());
            viewModel.UpdateRequested -= _requestUpdateHandler;
            viewModel.BuildTimetableRequested -= _requestBuildTimetableHandler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister timetable layout view model");
        }
    }

    public void UnregisterAll()
    {
        foreach (var vm in _vmDict.Values)
        {
            Unregister(vm);
        }
    }
}
