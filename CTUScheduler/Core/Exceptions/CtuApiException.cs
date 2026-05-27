using System;

namespace CTUScheduler.Core.Exceptions;

public class CtuApiException : Exception
{
    public CtuApiException(string message) : base(message)
    {
    }

    public CtuApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}