using System.Reactive;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Services.Dialogs;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.TimetableDialog;

public class TimetableDialogService : ITimetableDialogService
{
    private readonly ILogger<TimetableDialogService> _logger;
    private readonly IDialogHostService _dialogHostService;

    public TimetableDialogService(ILogger<TimetableDialogService> logger, IDialogHostService dialogHostService)
    {
        _logger = logger;
        _dialogHostService = dialogHostService;
    }

    public async Task ShowTimetableDetails<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        _logger.LogInformation("Opening timetable details");

        await _dialogHostService.ShowDialogAsync<TViewModel, Unit>(
            viewModel,
            DialogIdentifier.Timetable,
            false);

        _logger.LogInformation("Closed timetable details");
    }
}