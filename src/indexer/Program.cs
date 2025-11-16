using Gfd.Services;
using Gfd.Data;
using Gfd.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")!;
var poolSize = builder.Configuration.GetValue<int>("Db:PoolSize", 16);

builder.Services.AddPooledDbContextFactory<GfdDbContext>(
    o => o
        .UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
    poolSize);

builder.Services.AddSingleton<IGfdDataService>(_ => new GfdDataService(cs, poolSize));
builder.Services.AddSingleton<LanguageModel>();
builder.Services.AddSingleton<RabbitMqPuller>(services => {
    IConfigurationSection options = builder.Configuration.GetSection("RabbitMQ");
    return new RabbitMqPuller(
        options["host"],
        options["username"],
        options["password"],
        options["virtualhost"],
        int.Parse(options["port"]),
        int.Parse(options["connectionPoolSize"])
    );
});

var app = builder.Build();
app.Run();

class IndexingService : BackgroundService
{
    private const string IndexingQueueName = "IndexingQueue";
    private const int IdleTimeWhenQueueIsEmpty = 20;

    private IGfdDataService _dataService;
    private LanguageModel _embeddingService;
    private RabbitMqPuller _puller;

    public IndexingService(
        IGfdDataService dataService,
        LanguageModel embeddingService,
        RabbitMqPuller puller) 
    {
        _dataService = dataService;
        _embeddingService = embeddingService;
        _puller = puller;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) 
        {
            IndexingData? data = await _puller.PullWithPriorityAsync<IndexingData>(IndexingQueueName, stoppingToken);

            if (data is null) 
            {
                await Task.Delay(IdleTimeWhenQueueIsEmpty);
                continue;
            }

            WebsiteRecord record = _embeddingService.IndexingDataToWebsiteRecord(data);
            await _dataService.UpsertWebsiteRecordAsync(record);
        }
    }
}
