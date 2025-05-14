using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable
{
    public class HandmadeFindCourse : ViewModelBase, IDisposable, IRoutableViewModel
    {
        private readonly ICTUWebDriverService _ctuWebDriverService;


        public string? UrlPathSegment => "Handmade_Find_Course";

        public IScreen HostScreen { get; }

        public HandmadeFindCourse(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _ctuWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
