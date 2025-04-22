using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Exceptions
{
    public class DuplicateModuleException: Exception
    {
        public DuplicateModuleException() : base() { }
        public DuplicateModuleException(string message) : base(message) { }
        public DuplicateModuleException(string message, Exception innerException) : base(message, innerException) { }
    }
}
