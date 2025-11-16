using Gfd.Data;
using Gfd.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        var builder = WebApplication.CreateBuilder(args);

        var cs = builder.Configuration.GetConnectionString("Default");
        var poolSize = builder.Configuration.GetValue<int>("Db:PoolSize", 16);

        builder.Services.AddPooledDbContextFactory<GfdDbContext>(
            o => o
                .UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
            poolSize);

        builder.Services.AddSingleton<IGfdDataService>(_ => new GfdDataService(cs, poolSize));
        
        builder.Host.UseSerilog();
        builder.Services.AddControllers();
        builder.Services.AddControllersWithViews();
        builder.Services.AddSingleton<LanguageModel>();
        var app = builder.Build();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
