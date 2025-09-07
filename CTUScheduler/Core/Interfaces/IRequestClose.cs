using System;

namespace CTUScheduler.Core.Interfaces;

public interface IRequestClose
{
    event Action<object?>? RequestClose;
}