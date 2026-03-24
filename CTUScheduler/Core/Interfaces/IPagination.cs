using System.Collections.Generic;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface IPagination
{
    public int TotalPages { get; }
    public int PageSize { get; }
    public int CurrentPage { get; }
    public bool IsFirstPage { get; }
    public bool IsLastPage { get; }
}