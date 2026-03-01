using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Shared.Interfaces;

public interface ITransfer<in TSource,in TTarget> 
    where TSource: class
    where TTarget: class
{
    public Task Transfer(TSource source, TTarget target);
}