using System;

namespace CTUScheduler.Infrastructure.Services.MainHomeService;

public interface IMainHomeService
{
    IObservable<string> StudentIdChanges { get; }
}