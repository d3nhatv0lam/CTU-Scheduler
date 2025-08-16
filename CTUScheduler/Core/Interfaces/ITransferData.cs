using ReactiveUI;

namespace CTUScheduler.Core.Interfaces;

public interface ITransferData<in TSource,in TTarget> 
    where TSource: class
    where TTarget: class
{
    public void Transfer(TSource source, TTarget target);
}