using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface ICleanupAsync
{
    public Task CleanupAsync();
}