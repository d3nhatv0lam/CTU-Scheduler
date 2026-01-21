namespace CTUScheduler.Core.Interfaces;

public interface IInitializable<in TContext>
{
    void Initialize(TContext context);
}