using System.Threading.Tasks;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Models.Dialogs;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;

public interface IDialogService
{
    Task ShowAlert(string title, string message);

    Task<bool> ShowConfirm(string title, string message);

    void Show<TViewModel>(TViewModel viewModel, in DialogOptions options = default) where TViewModel : class;

    Task<TResult?> ShowModal<TViewModel, TResult>(TViewModel viewModel, in DialogOptions options = default)
        where TViewModel : class;
}