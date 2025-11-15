internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
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
