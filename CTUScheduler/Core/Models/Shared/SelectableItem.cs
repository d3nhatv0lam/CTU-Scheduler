using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared
{
    public class SelectableItem<T>: ReactiveObject, ISelectable
    {
        private bool _isSelected;
        private T _item;

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
        public T Item
        {
            get => _item;
            set => this.RaiseAndSetIfChanged(ref _item, value);
        }
        public SelectableItem(T item, bool isSelected = false)
        {
            _item = item;
            _isSelected = isSelected;
        }
    }
}
