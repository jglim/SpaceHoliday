using JetBrains.Annotations;
using JetBrains.Space.AspNetCore.Experimental.WebHooks;
using JetBrains.Space.AspNetCore.Experimental.WebHooks.Options;
using JetBrains.Space.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SpaceHoliday.Database;

namespace SpaceHoliday.WebHook;

[UsedImplicitly]
public partial class SpaceHolidayWebHookHandler : SpaceWebHookHandler
{
    private readonly SpaceDb _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SpaceHolidayWebHookHandler> _logger;
    
    public SpaceHolidayWebHookHandler(
        SpaceDb db,
        IMemoryCache cache,
        ILogger<SpaceHolidayWebHookHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<SpaceWebHookOptions> ConfigureRequestValidationOptionsAsync(SpaceWebHookOptions options, ApplicationPayload payload)
    {
        // When the payload has a clientId, configure request validation to use the signing key of the matching organization.
        var clientId = payload.GetClientId();
        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ClientId == clientId);
        if (organization != null)
        {
            options.ClientId = organization.ClientId;
            options.ClientSecret = organization.ClientSecret;
            options.ServerUrl = new Uri(organization.ServerUrl);
            options.VerifySigningKey = new VerifySigningKeyOptions
            {
                IsEnabled = true,
                EndpointSigningKey = organization.SigningKey
            };
            return options;
        }

        return await base.ConfigureRequestValidationOptionsAsync(options, payload);
    }
}