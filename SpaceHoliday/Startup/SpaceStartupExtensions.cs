using SpaceHoliday.WebHook;

namespace SpaceHoliday.Startup;

public static class SpaceStartupExtensions
{
    public static WebApplicationBuilder ConfigureSpaceHolidayWebHook(this WebApplicationBuilder builder)
    {
        builder.Services.AddSpaceWebHookHandler<SpaceHolidayWebHookHandler>();
        builder.Services.AddHostedService<LogSpaceHolidayRegistrationUrlsTask>();
        
        return builder;
    }
    
    public static WebApplication MapSpaceHolidayWebHook(this WebApplication app)
    {
        app.MapSpaceWebHookHandler<SpaceHolidayWebHookHandler>("/api/space");

        return app;
    }
}