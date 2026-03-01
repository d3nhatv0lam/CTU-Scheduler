using System;

namespace CTUScheduler.Presentation.Shared.Interfaces;

public interface IRequestUpdate<T> where T: class
{
    public event Action<T>? UpdateRequested;
}