using System;

namespace CTUScheduler.Core.Interfaces;

public interface ICloseableDialog
{
    event Action<object?>? RequestClose;
}