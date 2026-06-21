using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Base;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.TermsAcceptance.ViewModels;

public partial class TermsAcceptanceViewModel : ViewModelBase, IDialogContext, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<TermsAcceptanceViewModel> _logger;
    private event EventHandler<object?>? _requestClose;
    
    [Reactive] private bool _isAgreed;
    
    public ReactiveCommand<Unit, Unit> AcceptCommand { get; }
    public ReactiveCommand<Unit, Unit> DeclineCommand { get; }

    public TermsAcceptanceViewModel(ILogger<TermsAcceptanceViewModel> logger)
    {
        _logger = logger;
        var canAccept = this.WhenAnyValue(x => x.IsAgreed);
        AcceptCommand = ReactiveCommand.Create(() => Close(true), canAccept).DisposeWith(_disposables);
        DeclineCommand = ReactiveCommand.Create(() => Close(false)).DisposeWith(_disposables);
    }

    public void Close()
    {
        _requestClose?.Invoke(this, null);
    }

    public void Close(object result)
    {
        _requestClose?.Invoke(this, result);
    }

    public event EventHandler<object?>? RequestClose
    {
        add => _requestClose += value;
        remove => _requestClose -= value;
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("Disposed");
    }
}