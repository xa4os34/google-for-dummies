using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Parser;
using Gfd.Models;
using Gfd.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;

namespace Gfd.Crawler.Service;

public class Crawler : BackgroundService
{
    private readonly ILogger<Crawler> _logger;
    private readonly IRabbitMqPuller _rabbitMqPuller;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly SemaphoreSlim _concurrencyLimiter = new(100);
    public Crawler(ILogger<Crawler> logger, IRabbitMqPuller rabbitMqPuller, IRabbitMqPublisher rabbitMqPublisher,
        IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _rabbitMqPuller = rabbitMqPuller;
        _rabbitMqPublisher = rabbitMqPublisher;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int retryDelayMs = 5000; // 5 секунд между попытками
        
        _logger.LogInformation("Crawler service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (priority, record) = await _rabbitMqPuller.PullWithPriorityAsync<CrawlnigRecord>("CrawlingQueue", stoppingToken);
                if (record is null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Received URL from queue: {Url} (Priority: {Priority})", record.Url, priority);

                await _concurrencyLimiter.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleUrl(priority, record);
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, stoppingToken);
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex, "RabbitMQ is not reachable, retrying in {Delay}ms", retryDelayMs);
                await Task.Delay(retryDelayMs, stoppingToken);
            }
            catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
            {
                // Queue doesn't exist yet, will be created automatically on next attempt
                _logger.LogDebug("Queue not found, will be created automatically. Retrying in {Delay}ms", retryDelayMs);
                await Task.Delay(retryDelayMs, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in crawler loop");
                await Task.Delay(retryDelayMs, stoppingToken);
            }
        }
    }
    public async Task HandleUrl(MessagePriorityLevel priority, CrawlnigRecord record)
    {
        Uri uri = new Uri(record.Url);
        try
        {
            _logger.LogInformation("Processing URL: {Url}", record.Url);
            
            if (uri.IsBaseUrlAndRobotsTxt())
            {
                _logger.LogInformation("Processing robots.txt for: {Url}", record.Url);
                var crawlingFeedback = await ProccessRobotsTxt(uri);

                _logger.LogInformation("Found {SitemapCount} sitemaps for {Url}", crawlingFeedback.Sitemaps.Count, record.Url);
                await _rabbitMqPublisher.PublishAsync("CrawlingQueue", crawlingFeedback);
                _logger.LogInformation("Published crawling feedback to CrawlingQueue for {Url}", record.Url);
            }
            else
            {
                _logger.LogInformation("Crawling page: {Url}", record.Url);
                var indexingData = await ProccesDefault(uri);

                if (!string.IsNullOrEmpty(indexingData.Title))
                {
                    _logger.LogInformation("Extracted data from {Url}: Title='{Title}', Description length={DescLength}, Text length={TextLength}", 
                        record.Url, indexingData.Title, indexingData.Description?.Length ?? 0, indexingData.PageText?.Length ?? 0);
                }
                else
                {
                    _logger.LogWarning("No data extracted from {Url}", record.Url);
                }

                await _rabbitMqPublisher.PublishAsync("IndexingQueue", priority, indexingData);
                _logger.LogInformation("Published indexing data to IndexingQueue for {Url} (Priority: {Priority})", record.Url, priority);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing URL {Url}: {ErrorMessage}", record.Url, e.Message);
        }
    }
    private async Task<CrawlingFeedback> ProccessRobotsTxt(Uri uri)
    {
        string baseUri = uri.GetLeftPart(UriPartial.Authority);
        string sitemapUrl = $"{baseUri}/sitemap.xml";
        string robotsTxtUrl = $"{baseUri}/robots.txt";

        CrawlingFeedback crawlingFeedback = new(uri);
        var client = _httpClientFactory.CreateClient("GfdClient");

        _logger.LogDebug("Fetching robots.txt from {RobotsTxtUrl}", robotsTxtUrl);
        try
        {
            using var responseRobots = await client.GetAsync(robotsTxtUrl);
            if (responseRobots.StatusCode != HttpStatusCode.NotFound && responseRobots.Content.IsPlainText())
            {
                var robotsContent = await responseRobots.Content.ReadAsStringAsync();
                RobotsTxtParser.Parse(robotsContent, crawlingFeedback);
                _logger.LogDebug("Parsed robots.txt, found {SitemapCount} sitemaps", crawlingFeedback.Sitemaps.Count);
            }
            else
            {
                _logger.LogDebug("robots.txt not found or not plain text for {BaseUri}", baseUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch robots.txt from {RobotsTxtUrl}", robotsTxtUrl);
        }

        // Дополнительная проверка для поиска sitemap по стандартному пути
        _logger.LogDebug("Checking for sitemap at {SitemapUrl}", sitemapUrl);
        try
        {
            using var responseSitemap = await client.GetAsync(sitemapUrl);
            if (responseSitemap.StatusCode != HttpStatusCode.NotFound && !crawlingFeedback.Sitemaps.Contains(sitemapUrl))
            {
                crawlingFeedback.Sitemaps.Add(sitemapUrl);
                _logger.LogDebug("Added default sitemap: {SitemapUrl}", sitemapUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check sitemap at {SitemapUrl}", sitemapUrl);
        }

        return crawlingFeedback;
    }
    private async Task<IndexingData> ProccesDefault(Uri uri)
    {
        IndexingData indexingData = new IndexingData(uri.ToString());
        var client = _httpClientFactory.CreateClient("GfdClient");

        _logger.LogDebug("Fetching page: {Url}", uri);
        using var response = await client.GetAsync(uri);

        if (response.StatusCode == HttpStatusCode.NotFound || !response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch {Url}: Status {StatusCode}", uri, response.StatusCode);
            return indexingData;
        }

        string htmlContent = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning("Empty content received from {Url}", uri);
            return indexingData;
        }

        _logger.LogDebug("Parsing HTML content from {Url}, size: {Size} bytes", uri, htmlContent.Length);
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        // Извлекаем Title
        var titleElement = document.QuerySelector("title");
        if (titleElement is not null)
        {
            indexingData.Title = titleElement.TextContent.Trim();
            _logger.LogDebug("Extracted title: {Title}", indexingData.Title);
        }

        // Извлекаем Description из meta тега
        var metaDescription = document.QuerySelector("meta[name='description']");
        if (metaDescription is not null)
        {
            indexingData.Description = metaDescription.GetAttribute("content")?.Trim();
            _logger.LogDebug("Extracted description, length: {Length}", indexingData.Description?.Length ?? 0);
        }

        // Извлекаем весь текст страницы (без HTML тегов)
        // Удаляем скрипты и стили для чистоты текста
        var scriptElements = document.QuerySelectorAll("script, style");
        foreach (var element in scriptElements)
        {
            element.Remove();
        }

        indexingData.PageText = document.Body?.TextContent?.Trim() ?? string.Empty;
        _logger.LogDebug("Extracted page text, length: {Length} characters", indexingData.PageText.Length);

        return indexingData;
    }
}