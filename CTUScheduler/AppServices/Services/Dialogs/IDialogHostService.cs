using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Dialogs
{
    public interface IDialogHostService
    {
        Task<T?> ShowDialog<T>(object viewModel, DialogHostService.DialogIdentifier identifier);
    }
}
