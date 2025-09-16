using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.Adapter;

public class TimetableLayoutVmAdapter: ITimetableLayoutAdapter
{
    private readonly IScheduleManagerService _scheduleManagerService;
    private readonly ILogger<TimetableLayoutVmAdapter> _logger;
    private readonly Dictionary<ScheduleTable,TimetableLayoutViewModel> _vmDict = new();

    private readonly Action<TimetableLayoutViewModel> _requestUpdateHandler;
    
    public TimetableLayoutVmAdapter(IScheduleManagerService scheduleManagerService, ILogger<TimetableLayoutVmAdapter> logger)
    {
        _scheduleManagerService = scheduleManagerService;
        _logger = logger;
        _requestUpdateHandler =  vm =>
        {
            var list = vm.GetScheduleData();
            foreach (var (code,group) in list)
            {
                var section = _scheduleManagerService.GetCourseSection(code, group);
                if (section is null) continue;
                vm.UpdateTimetableData(section);
            }
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
        await _scheduleManagerService.ReloadCourseDataAsync();
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
            viewModel.RequestUpdate += _requestUpdateHandler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register timetable layout view model");
        }
    }

    public void Unregister(TimetableLayoutViewModel viewModel)
    {
        if (viewModel is null) return;
        try
        {
            _vmDict.Remove(viewModel.ToModel());
            viewModel.RequestUpdate -= _requestUpdateHandler;
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
