using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Exceptions
{
    public class ConflictModuleException: Exception
    {
        public ConflictModuleException(): base() { }
        public ConflictModuleException(string message) : base(message) { }
        public ConflictModuleException(string message, Exception innerException) : base(message, innerException) { }
    }
}
