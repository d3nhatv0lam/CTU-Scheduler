using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Exceptions
{
    public class NoInternetException: Exception
    {
        public NoInternetException(string message) : base(message)
        {
        }
        public NoInternetException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public NoInternetException()
        {
        }
    }
}
