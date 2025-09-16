using System;

namespace CTUScheduler.Core.Interfaces;

public interface IRequestUpdate<T> where T: class
{
    public event Action<T>? UpdateRequested;
}