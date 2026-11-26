using System;

namespace CTUScheduler.Core.Exceptions;

public class CaptchaBlockedException : Exception
{
    public CaptchaBlockedException(string message) : base(message)
    {
    }

    public CaptchaBlockedException()
    {
    }
}
