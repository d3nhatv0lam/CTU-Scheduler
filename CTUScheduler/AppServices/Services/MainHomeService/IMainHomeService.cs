using System;

namespace CTUScheduler.AppServices.Services.MainHomeService;

public interface IMainHomeService
{
    IObservable<string> StudentIdChanges { get; }
}