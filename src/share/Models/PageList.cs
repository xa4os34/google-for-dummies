using System.Collections.Generic;

namespace Gfd.Models;

public class PageList
{
    public IEnumerable<WebsiteRecord> Results { get; set; } = new List<WebsiteRecord>();
    public long TotalCount { get; set; }
}
