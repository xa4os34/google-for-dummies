using Gfd.Services;
using Gfd.Crawler.Service;
using Serilog;

internal class Program
{
	public static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
		   .WriteTo.Console()
		   .CreateLogger();
		var builder = WebApplication.CreateBuilder(args);

		var cs = builder.Configuration;

		var host = cs["RabbitMQ:Host"] ?? "localhost";
		var user = cs["RabbitMQ:User"] ?? "guest";
		var pass = cs["RabbitMQ:Password"] ?? "guest";
		var vhost = cs["RabbitMQ:VHost"] ?? "/";
		var port = int.TryParse(cs["RabbitMQ:Port"], out var p) ? p : 5672;
		var pool = int.TryParse(cs["RabbitMQ:PoolSize"], out var s) ? s : 8;

		builder.Host.UseSerilog();
		builder.Services.AddMemoryCache();
		builder.Services.AddHttpClient("GfdClient", client =>
		{
			client.DefaultRequestHeaders.UserAgent.ParseAdd("GfdCrawler/1.0 (github.com/xa4os34/google-for-dummies)");
		});
		builder.Services.AddSingleton<IRabbitMqPublisher>(_ =>
			new RabbitMqPublisher(host, user, pass, vhost, port, pool));

		builder.Services.AddSingleton<IRabbitMqPuller>(_ =>
			new RabbitMqPuller(host, user, pass, vhost, port, pool));

		builder.Services.AddHostedService<Crawler>();

		var app = builder.Build();
		await app.RunAsync();
	}
}
