﻿using JetBrains.Space.Client;
using Microsoft.EntityFrameworkCore;
using SpaceHoliday.Database;

namespace SpaceHoliday.WebHook;

public partial class SpaceHolidayWebHookHandler
{
    public override async Task<ApplicationExecutionResult> HandleInitAsync(InitPayload payload)
    {
        // Validation
        if (payload.State == null)
        {
            _logger.LogWarning("No state parameter is provided in the in the request payload");
            return new ApplicationExecutionResult("No state parameter is provided in the request payload.", 400);
        }

        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ServerUrl == payload.ServerUrl);
        if (organization != null)
        {
            _logger.LogWarning("The organization server URL is already registered. ServerUrl={ServerUrl}; Existing ClientId={ClientId}; New ClientId={NewClientId}", payload.ServerUrl, organization.ClientId, payload.ClientId);
            return new ApplicationExecutionResult("The organization server URL is already registered.", 400);
        }

        // Create organization locally
        organization = new Organization
        {
            Created = DateTimeOffset.UtcNow,
            ServerUrl = payload.ServerUrl,
            ClientId = payload.ClientId,
            ClientSecret = payload.ClientSecret,
            UserId = payload.UserId,
            SigningKey = "pending"
        };

        _db.Organizations.Add(organization);

        await _db.SaveChangesAsync();
        
        // Connect to Space
        var connection = organization.CreateConnection();
        var applicationClient = new ApplicationClient(connection);

        // Store signing key
        var signingKey = await applicationClient.SigningKey.GetSigningKeyAsync(ApplicationIdentifier.Me);
        organization.SigningKey = signingKey;
        await _db.SaveChangesAsync();
        
        // Initialize Space organization
        var applicationInfo = await applicationClient.GetApplicationAsync(ApplicationIdentifier.Me);

        if (string.IsNullOrEmpty(applicationInfo.Picture))
        {
            await using var logoStream = GetType().Assembly.GetManifestResourceStream("SpaceHoliday.Resources.logo.png")!;
            
            var uploadClient = new UploadClient(connection);
            var uploadedFileAttachmentId = await uploadClient.UploadAsync(
                storagePrefix: "file",
                fileName: "logo.png",
                uploadStream: logoStream,
                mediaType: null);
        
            if (!string.IsNullOrEmpty(uploadedFileAttachmentId))
            {
                await applicationClient.UpdateApplicationAsync(
                    application: ApplicationIdentifier.Me,
                    pictureAttachmentId: uploadedFileAttachmentId);
            }
        }
            
        await applicationClient.Authorizations.AuthorizedRights.RequestRightsAsync(
            application: ApplicationIdentifier.Me,
            contextIdentifier: PermissionContextIdentifier.Global, 
            rightCodes: new List<string>
            {
                "Profile.View",
                "Channel.ViewMessages",
                "Channel.ViewChannel"
            });
        
        return await base.HandleInitAsync(payload);
    }

    public override async Task<ApplicationExecutionResult> HandleChangeClientSecretRequestAsync(ChangeClientSecretPayload payload)
    {
        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ClientId == payload.ClientId);
        if (organization == null)
        {
            _logger.LogWarning("The organization does not exist. ClientId={ClientId}", payload.ClientId);
            return new ApplicationExecutionResult("The organization does not exist.", 400);
        }

        organization.ClientSecret = payload.NewClientSecret;
        await _db.SaveChangesAsync();
        
        return await base.HandleChangeClientSecretRequestAsync(payload);
    }

    public override async Task<ApplicationExecutionResult> HandleChangeServerUrlAsync(ChangeServerUrlPayload payload)
    {
        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ClientId == payload.ClientId);
        if (organization == null)
        {
            _logger.LogWarning("The organization does not exist. ClientId={ClientId}", payload.ClientId);
            return new ApplicationExecutionResult("The organization does not exist.", 400);
        }

        organization.ServerUrl = payload.NewServerUrl;
        await _db.SaveChangesAsync();
            
        return await base.HandleChangeServerUrlAsync(payload);
    }
}