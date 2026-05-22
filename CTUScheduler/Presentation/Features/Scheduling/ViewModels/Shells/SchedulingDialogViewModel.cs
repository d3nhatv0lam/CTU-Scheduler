using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Irihi.Avalonia.Shared.Contracts;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;

public partial class SchedulingDialogViewModel : ViewModelBase, IScreen, IDisposable, IDialogContext
{
    private readonly CompositeDisposable _disposables = new();

    public event EventHandler<object?>? RequestClose;
    public void Close() => RequestClose?.Invoke(this, null);

    public RoutingState Router { get; } = new();
    public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; }

    [ObservableAsProperty] private bool _isSelectionMode;

    public SchedulingDialogViewModel(INavigationRegionManager navigationRegionManager)
    {
        navigationRegionManager.Register(RegionIds.Scheduling, this)
            .DisposeWith(_disposables);

        CloseDialogCommand = ReactiveCommand.Create(Close)
            .DisposeWith(_disposables);

        _isSelectionModeHelper = Router.CurrentViewModel
            .Select(currentVm => currentVm is SelectionViewModel)
            .ToProperty(this, x => x.IsSelectionMode)
            .DisposeWith(_disposables);

        navigationRegionManager.NavigateTo<SelectionViewModel>(RegionIds.Scheduling);

        var interactionDisposable = new SerialDisposable().DisposeWith(_disposables);

        Router
            .CurrentViewModel
            .Subscribe(childVm =>
            {
                if (childVm is IHaveCloseInteraction<Unit> closeable)
                {
                    interactionDisposable.Disposable = closeable.CloseInteraction
                        .RegisterHandler(context =>
                            {
                                Close();
                                context.SetOutput(Unit.Default);
                            }
                        );
                }
                else
                {
                    interactionDisposable.Disposable = Disposable.Empty;
                }
            })
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}