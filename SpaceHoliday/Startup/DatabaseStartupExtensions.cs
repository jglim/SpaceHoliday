using Microsoft.EntityFrameworkCore;
using SpaceHoliday.Database;

namespace SpaceHoliday.Startup;

public static class DatabaseStartupExtensions
{
    public static WebApplication EnsureDb(this WebApplication app)
    {
        using var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<SpaceDb>();
        if (db.Database.IsRelational())
        {
            app.Logger.LogInformation("Updating database...");
            db.Database.Migrate();
            app.Logger.LogInformation("Updated database");
        }

        return app;
    }
}