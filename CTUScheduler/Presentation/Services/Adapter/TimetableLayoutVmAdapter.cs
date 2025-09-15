using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Presentation.Services.Adapter;

public class TimetableLayoutVmAdapter: ITimetableLayoutAdapter
{
    private readonly IScheduleManagerService _scheduleManagerService;
    private readonly List<TimetableLayoutViewModel> _vmList = new();

    private readonly Action<TimetableLayoutViewModel> _requestUpdateHandler;
    
    public TimetableLayoutVmAdapter(IScheduleManagerService scheduleManagerService)
    {
        _scheduleManagerService = scheduleManagerService;
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
    
    public void GetOrCreateLayout(ScheduleTable table)
    {
        
    }
    
    public async Task UpdateAsync()
    {
        await _scheduleManagerService.ReloadCourseDataAsync();
        foreach (var vm in _vmList)
        {
            vm.Update();
        }
    }

    public void Register(TimetableLayoutViewModel viewModel)
    {
        _vmList.Add(viewModel);
        viewModel.RequestUpdate += _requestUpdateHandler;
    }

    public void Unregister(TimetableLayoutViewModel viewModel)
    {
        _vmList.Remove(viewModel);
        viewModel.RequestUpdate -= _requestUpdateHandler;
    }

    public void UnregisterAll()
    {
        foreach (var vm in _vmList)
        {
            Unregister(vm);
        }
    }
}