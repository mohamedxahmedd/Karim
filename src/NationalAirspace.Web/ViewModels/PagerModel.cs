using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace NationalAirspace.Web.ViewModels;

/// <summary>Pagination links that preserve the current query string.</summary>
public sealed class PagerModel(int page, int totalPages, HttpRequest request)
{
    public int Page { get; } = page;
    public int TotalPages { get; } = totalPages;

    public string UrlFor(int page)
    {
        var query = QueryHelpers.ParseQuery(request.QueryString.Value);
        var builder = new QueryBuilder();
        foreach (var (key, values) in query)
        {
            if (string.Equals(key, "page", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var v in values) builder.Add(key, v ?? string.Empty);
        }
        if (page > 1) builder.Add("page", page.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return request.PathBase + request.Path + builder.ToQueryString();
    }

    /// <summary>Compact page window, e.g. 1 … 4 5 6 … 12 (0 = ellipsis).</summary>
    public IEnumerable<int> Window()
    {
        var pages = new SortedSet<int> { 1, TotalPages };
        for (var p = Page - 2; p <= Page + 2; p++) if (p >= 1 && p <= TotalPages) pages.Add(p);
        var last = 0;
        foreach (var p in pages)
        {
            if (last != 0 && p - last > 1) yield return 0;
            yield return p;
            last = p;
        }
    }
}
