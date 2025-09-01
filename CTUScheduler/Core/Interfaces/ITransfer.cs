using System.Threading.Tasks;
using ReactiveUI;

namespace CTUScheduler.Core.Interfaces;

public interface ITransfer<in TSource,in TTarget> 
    where TSource: class
    where TTarget: class
{
    public Task Transfer(TSource source, TTarget target);
}