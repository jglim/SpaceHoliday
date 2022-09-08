using SpaceHoliday.Database;
using SpaceHoliday.Startup;
using SpaceHoliday.WebHook;

var builder = WebApplication.CreateBuilder(args);

// as this will be deployed on fly.io, bind to port 8080 where it will be exposed via their reverse-proxy
// application runs on their free tier, using approx 120MB RAM on startup
builder.WebHost.UseUrls("http://0.0.0.0:8080");
builder.Services.AddSqlite<SpaceDb>("Data Source=application.db;Cache=Shared");
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddMemoryCache();
builder.ConfigureSpaceHolidayWebHook();

var app = builder.Build();
app.EnsureDb();
app.MapSpaceHolidayWebHook();
app.MapGet("/", () => $"SpaceHoliday is running.");
app.MapGet("/install", () => $"{LogSpaceHolidayRegistrationUrlsTask.RegUrls}");

app.Run();