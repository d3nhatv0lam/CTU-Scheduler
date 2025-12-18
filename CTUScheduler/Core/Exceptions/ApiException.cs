using System;

namespace CTUScheduler.Core.Exceptions;

public class ApiException: Exception
{
    public int Code { get; }
    public ApiException(int code, string? message) : base(message)
    {
        Code = code;
    }
}