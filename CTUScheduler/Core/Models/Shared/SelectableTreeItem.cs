using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared
{
    public class SelectableTreeItem<T> : SelectableItem<T>
    {
        public SelectableTreeItem(T item) : base(item)
        {

        }
    }
}
