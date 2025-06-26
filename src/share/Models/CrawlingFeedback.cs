
namespace Gfd.Models;

public class CrawlingFeedback 
{
    public required Uri BaseUrl { get; init; }

    public required Uri[] NewUrls { get; init; } = new Uri[0];

    public Uri[] DisallowedUrls  { get; init; } = new Uri[0];
    
    public TimeSpan? newCrawlingDelay { get; init; }
}
