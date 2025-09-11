using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Services.TimetableDialog;

public interface ITimetableDialogService
{
    public Task ShowTimetableDetails<TViewModel>(TViewModel viewModel) where TViewModel : class;
}