
namespace Gfd.Models;

public interface ICrawlingFeedback 
{
    public Uri BaseUrl { get; init; }
}

public class RobotsTxtFeedback : ICrawlingFeedback
{
    public Uri BaseUrl { get; init; }
    public int CrawlingDelay { get; set; }
    public List<string> Allowed { get; set; } = new();
    public List<string> DisAllowed { get; set; } = new();
    public List<string> Sitemaps { get; set; } = new();
    public RobotsTxtFeedback(Uri baseUrl) => BaseUrl = baseUrl;
}

public class NewUrlsFeedback : ICrawlingFeedback
{
    public Uri BaseUrl { get; init; }
    public List<string> Urls { get; set; } = new();
    public NewUrlsFeedback(Uri baseUrl) => BaseUrl = baseUrl;
}
