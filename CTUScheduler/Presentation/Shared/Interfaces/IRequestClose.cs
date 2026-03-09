using System;

namespace CTUScheduler.Presentation.Shared.Interfaces;

public interface IRequestClose
{
    event Action<object?>? RequestClose;
    void Close(Object? result = null);
}