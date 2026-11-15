using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PersonalFitnessAgent.Infrastructure.Sqlite;

namespace PersonalFitnessAgent.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "fitness-agent.db");
        builder.Services.AddSingleton(new SqliteLocalStore(databasePath));
        builder.Services.AddSingleton<FitnessDataService>();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        App.Data = app.Services.GetRequiredService<FitnessDataService>();
        return app;
    }
}
