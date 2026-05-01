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

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;

public class SchedulingDialogViewModel : ViewModelBase, IScreen, IDisposable, IDialogContext
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public event EventHandler<object?>? RequestClose;
    public void Close() => RequestClose?.Invoke(this, null);

    public RoutingState Router { get; } = new();
    public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; }

    public SchedulingDialogViewModel(INavigationRegionManager navigationRegionManager)
    {
        navigationRegionManager.Register(RegionIds.Scheduling, this)
            .DisposeWith(_disposables);

        CloseDialogCommand = ReactiveCommand.Create(Close)
            .DisposeWith(_disposables);

        navigationRegionManager.NavigateTo<SelectionViewModel>(RegionIds.Scheduling);

        var interactionDisposable = new SerialDisposable().DisposeWith(_disposables);

        this.Router
            .CurrentViewModel
            .Subscribe(childVm =>
            {
                if (childVm is IHaveCloseInteraction<Unit> closeable)
                {
                    interactionDisposable.Disposable = closeable.CloseInteraction
                        .RegisterHandler(context =>
                            {
                                this.Close();
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