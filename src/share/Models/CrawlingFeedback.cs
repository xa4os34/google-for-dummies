
namespace Gfd.Models;

public class CrawlingFeedback 
{
    public Type Type { get; init; }

    public required Uri[] Urls { get; init; } = new Uri[0];

    public required Uri BaseUrl { get; init; }

}

public class RobotsTxtCrawlingFeedback : CrawlingFeedback
{
    public Uri[] AllowedUrls  { get; init; } = new Uri[0];

    public Uri[] DisallowedUrls  { get; init; } = new Uri[0];

}
