using GoogleForDummys.Share.Services;
using Serilog;
internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();
        builder.Services.AddSingleton<Serilog.ILogger>(_ => Log.Logger); // Возможно это не лучшее решение и проблема в share/Services/LanguageModel
        builder.Services.AddControllers();
        builder.Services.AddControllersWithViews();
        builder.Services.AddSingleton<LanguageModel>();
        var app = builder.Build();

        app.UseStaticFiles();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();

    }

}
