namespace Rvnx.CRM.Core.DTOs.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
}
