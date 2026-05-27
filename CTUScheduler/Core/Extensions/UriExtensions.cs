using System.Collections.Generic;
using System.Web;

namespace CTUScheduler.Core.Extensions;

public static class UriExtensions
{
    public static string ParseQueryString(this string uri, IDictionary<string, string> queryParams)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var kvp in queryParams)
        {
            query[kvp.Key] = kvp.Value;
        }
        

        return $"{uri}?{query}";
    }
}