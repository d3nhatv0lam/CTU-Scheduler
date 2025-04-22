using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }
    }
}
