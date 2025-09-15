using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface IUpdatableAsync
{
    public Task UpdateAsync();
}