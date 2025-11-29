using Gfd.Models;

public static class RobotsTxtParser
{
    public static void Parse(string robotsTxtSring, CrawlingFeedback crawlingFeedback)
    {
        string[] lines = robotsTxtSring.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string? userAgent = null;
        int crawlDelay = default;
        List<string> allow = new();
        List<string> disAllow = new();
        List<string> sitemaps = new();

        foreach (string line in lines)
        {
            var keyValuePair = line.GetKeyValuePair();
            if (string.IsNullOrEmpty(keyValuePair.Key) || string.IsNullOrEmpty(keyValuePair.Value))
                continue;

            if (keyValuePair.Key == "User-agent")
            {
                userAgent = keyValuePair.Value;
            }

            if (userAgent is not null && userAgent == "*" || userAgent == "Gfd")
            {
                switch (keyValuePair.Key)
                {
                    case "crawl-delay":
                        int.TryParse(keyValuePair.Value, out crawlDelay);
                        break;
                    case "Disallow":
                        disAllow.Add(keyValuePair.Value);
                        break;
                    case "Allow":
                        allow.Add(keyValuePair.Value);
                        break;
                    case "Sitemap":
                        sitemaps.Add(keyValuePair.Value);
                        break;
                }
            }
        }

        if (crawlDelay != default)
            crawlingFeedback.CrawlingDelay = crawlDelay;

        crawlingFeedback.Allowed = allow;
        crawlingFeedback.DisAllowed = disAllow;
        crawlingFeedback.Sitemaps = sitemaps;
    }
}