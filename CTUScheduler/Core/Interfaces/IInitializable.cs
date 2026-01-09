namespace CTUScheduler.Core.Interfaces;

public interface IInitializable<in TContext>
{
    void Init(TContext context);
}