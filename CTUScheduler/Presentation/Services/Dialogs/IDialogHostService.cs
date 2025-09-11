using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Services.Dialogs
{
    public interface IDialogHostService
    {
        Task<TResult?> ShowDialogAsync<TViewModel, TResult>(TViewModel viewModel,
            DialogIdentifier identifier, bool isDisposeViewModel = true) where TViewModel : class;
    }
}
