using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface ISearchable
{
    Task FillQueryAsync(string query);
    Task SearchAsync();
}