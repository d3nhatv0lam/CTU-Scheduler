using System;

namespace CTUScheduler.Core.Exceptions;

public class SessionExpiredException: Exception
{
    public SessionExpiredException(string message) : base(message) {}
    
    public SessionExpiredException(string message, Exception innerException) : base(message, innerException){}
    public SessionExpiredException() {}
}