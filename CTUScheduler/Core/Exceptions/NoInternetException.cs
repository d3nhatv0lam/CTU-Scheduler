using System;

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
