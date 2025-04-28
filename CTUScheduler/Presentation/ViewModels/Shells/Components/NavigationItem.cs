using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.Shells.Components
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
