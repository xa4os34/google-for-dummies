
namespace Gfd.Models;

public class CrawlingFeedback
{
    public Uri BaseUrl { get; init; }
    public int CrawlingDelay { get; set; }
    public List<string> Allowed { get; set; } = new();
    public List<string> DisAllowed { get; set; } = new();
    public List<string> Sitemaps { get; set; } = new();
    public CrawlingFeedback(Uri baseUrl) => BaseUrl = baseUrl;
}
