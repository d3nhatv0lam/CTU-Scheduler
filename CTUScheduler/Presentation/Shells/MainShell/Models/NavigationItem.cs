using System;

namespace CTUScheduler.Presentation.Shells.MainShell.Models
{
    public class NavigationItem
    {
        public string Title { get; }
        public Material.Icons.MaterialIconKind Kind { get; }
        public Type ViewModelType { get; }

        public NavigationItem(string title, Material.Icons.MaterialIconKind kind, Type viewModelType)
        {
            Title = title;
            Kind = kind;
            ViewModelType = viewModelType;
        }
    }
}
