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
        // jg : guard the deserialization, since app validation will crash with a nonexistent client id
        // GetClientId guard was raised with ClassName (AppPublicationCheckPayload) :
        // Specified argument was out of the range of valid values. (Parameter 'current')
        // > no_client_id, no_client_secret, bad_uri
        
        // post-submission (success) notes
        // AppPublicationCheckPayload was causing the crash, as it is sent with no content
        // This can be detected with something like : if (payload.ClassName == "AppPublicationCheckPayload") ..
        var clientId = "";
        try
        {
            clientId = payload.GetClientId();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetClientId guard was raised with ClassName ({payload.ClassName}) : {ex.Message}");
            var cid = options.ClientId ?? "no_client_id";
            var cs = options.ClientSecret ?? "no_client_secret";
            var uri = "bad_uri";
            if (options.ServerUrl != null)
            {
                uri = options.ServerUrl.AbsolutePath;
            }
            Console.WriteLine($"> {cid}, {cs}, {uri}");
        }

        // When the payload has a clientId, configure request validation to use the signing key of the matching organization.
        // var clientId = payload.GetClientId();
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