using System;

namespace CTUScheduler.AppServices.Abstractions;

public interface IMainHomeService
{
    IObservable<string> StudentIdChanges { get; }
}