using CTUScheduler.Presentation.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable.Interfaces
{
    public interface IStepViewModel
    {
        /// <summary>
        /// Null is last step!
        /// </summary>
        ViewModelBase? NextViewModel { get; }
    }
}
