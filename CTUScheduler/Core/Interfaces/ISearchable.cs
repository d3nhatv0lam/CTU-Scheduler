using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface ISearchable
{
    Task FillQueryAsync(string query, CancellationToken cancellationToken = default);
    Task SearchAsync(CancellationToken cancellationToken = default);
}