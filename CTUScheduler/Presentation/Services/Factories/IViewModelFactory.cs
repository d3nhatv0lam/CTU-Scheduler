using System;
using CTUScheduler.Presentation.Shared.Interfaces;

namespace CTUScheduler.Presentation.Services.Factories;

public interface IViewModelFactory
{
    IViewModel Create(Type vmType);
    IViewModel Create(Type vmType, object args);
}