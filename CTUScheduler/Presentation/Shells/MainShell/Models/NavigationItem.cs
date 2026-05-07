using System;
using Material.Icons;

namespace CTUScheduler.Presentation.Shells.MainShell.Models;

public readonly record struct NavigationItem(
    string Title,
    MaterialIconKind Kind,
    Type ViewModelType);