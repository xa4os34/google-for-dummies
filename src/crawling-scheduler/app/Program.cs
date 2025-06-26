using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Gfd.RabbitMQ;
using Gfd.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog();
builder.Services.AddHostedService<FeedbackHandlingService>();
builder.Services.AddHostedService<CrawlingSchedulingService>();


var app = builder.Build();
app.Run();

class FeedbackHandlingService : BasicQueuePollingService<CrawlingFeedback>
{
    public FeedbackHandlingService() 
        : BasicQueuePollingService()
    {

    }

    public void MessageHandler(CrawlingFeedback feedback) 
    {
    }
}

class CrawlingSchedulingService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {

    }
}
