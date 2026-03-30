using System;
using CTUScheduler.Presentation.Shared.Interfaces;

namespace CTUScheduler.Presentation.Services.Factories;

public interface IViewModelFactory
{
    IViewModel Create(Type vmType);
    // args can be null, and has type INeedArgs<T> args
    IViewModel Create(Type vmType, object args);
    // args can be null, and has type INeedArgs<T> args
    IViewModel Create(Type vmType, params object[] inputs);
}