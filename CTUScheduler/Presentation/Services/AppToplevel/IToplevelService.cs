using System;
using Avalonia.Controls;

namespace CTUScheduler.Presentation.Services.AppToplevel;

public interface IToplevelService
{
    IObservable<TopLevel?> ToplevelChanges { get; }
    void Initialize(Control root);
}