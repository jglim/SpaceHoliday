using JetBrains.Annotations;
using JetBrains.Space.Common.Applications;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace SpaceHoliday.WebHook;

[UsedImplicitly]
public class LogSpaceHolidayRegistrationUrlsTask : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServer _server;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogSpaceHolidayRegistrationUrlsTask> _logger;
    public static string RegUrls = "";
        
    public LogSpaceHolidayRegistrationUrlsTask(
        IHostApplicationLifetime applicationLifetime,
        IServer server,
        IConfiguration configuration,
        ILogger<LogSpaceHolidayRegistrationUrlsTask> logger)
    {
        _applicationLifetime = applicationLifetime;
        _server = server;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _applicationLifetime.ApplicationStarted.Register(ApplicationStarted);

        return Task.CompletedTask;
    }

    private void ApplicationStarted()
    {
        var serverAddresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses.ToList();
        if (serverAddresses == null || serverAddresses.Count == 0)
        {
            _logger.LogInformation("Could not determine server address");
            return;
        }

        var tunnelAddress = _configuration["TunnelAddress"];
        if (!string.IsNullOrEmpty(tunnelAddress))
        {
            serverAddresses.Add(tunnelAddress);
        }

        foreach (var serverAddress in serverAddresses)
        {
            var applicationInstallationUri = ApplicationUrlGenerator.GenerateInstallGenericUrl(
                applicationName: Constants.ApplicationName, 
                applicationEndpoint: new Uri(serverAddress + "/api/space"),
                state: "firstrun",
                authFlows: new []
                {
                    SpaceAuthFlow.ClientCredentials(),
                    SpaceAuthFlow.AuthorizationCode(
                        redirectUris: new []
                        {
                            new Uri(serverAddress)
                        },
                        pkceRequired: true),
                },
                authForMessagesFromSpace: AuthForMessagesFromSpace.SigningKey);

            _logger.LogInformation("URL to install the application to Space:\n{Url}", applicationInstallationUri.AbsoluteUri);
            if (!applicationInstallationUri.AbsoluteUri.Contains("localhost"))
            {
                RegUrls = applicationInstallationUri.AbsoluteUri;
            }
        }
    }
}