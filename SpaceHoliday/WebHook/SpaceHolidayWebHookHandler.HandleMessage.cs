using JetBrains.Space.Client;
using Microsoft.EntityFrameworkCore;
using SpaceHoliday.Holiday;

namespace SpaceHoliday.WebHook;

public partial class SpaceHolidayWebHookHandler
{
    public override async Task<Commands> HandleListCommandsAsync(ListCommandsPayload payload)
    {
        using var loggerScopeForClientId = _logger.BeginScope("ClientId={ClientId}", payload.ClientId);
        
        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ClientId == payload.ClientId);
        if (organization == null)
        {
            _logger.LogWarning("The organization does not exist. ClientId={ClientId}", payload.ClientId);
            return new Commands();
        }

        return GetCommands();
    }

    public override async Task HandleMessageAsync(MessagePayload payload)
    {
        using var loggerScopeForClientId = _logger.BeginScope("ClientId={ClientId}", payload.ClientId);
        
        var organization = await _db.Organizations.FirstOrDefaultAsync(it => it.ClientId == payload.ClientId);
        if (organization == null)
        {
            _logger.LogWarning("The organization does not exist. ClientId={ClientId}", payload.ClientId);
            return;
        }
        
        if (payload.Message.Body is not ChatMessageText messageText || string.IsNullOrEmpty(messageText.Text))
        {
            _logger.LogWarning("Unknown payload message body type. MessageBodyType={MessageBodyType}", payload.Message.Body?.GetType());
            return;
        }
        var organizationConnection = organization.CreateConnection();
        var organizationChatClient = new ChatClient(organizationConnection);
        
        var trimmedText = messageText.Text.Trim().ToLower();
        
        if (trimmedText.StartsWith("next"))
        {
            await HandleNextHolidayAsync(payload, organizationChatClient);
            return;
        }
        else if (trimmedText.StartsWith("status"))
        {
            await HandleStatusAsync(payload, organizationChatClient);
            return;
        }

        await HandleHelpAsync(payload, organizationChatClient);
    }
    
    private async Task HandleNextHolidayAsync(MessagePayload payload, ChatClient chatClient)
    {
        string reply = "No future holiday entries could be found.";
        var today = DateTime.Today;
        
        var prunedTable = HolidayData.GetPrunedHolidayEntries().Take(3).ToArray();
        HolidayEntry nextHoliday = (prunedTable.Length > 0) ? prunedTable[0] : null;
        if (nextHoliday != null)
        {
            // int daysTillHoliday = nextHoliday.Date.Subtract(today).Days;
            reply = $"Next holiday: {nextHoliday.Name}";
            
            await chatClient.Messages.SendMessageAsync(
                recipient: MessageRecipient.Member(ProfileIdentifier.Id(payload.UserId)),
                content: ChatMessage.Block(
                    outline: new MessageOutline(reply, new ApiIcon("smile")),
                    sections: new List<MessageSectionElement>()
                    {
                        MessageSectionElement.MessageSection(
                            header: "Upcoming holidays",
                            elements: new List<MessageBlockElement>
                            {
                                MessageBlockElement.MessageFields(
                                    prunedTable.Select((row) =>
                                        {
                                            int daysToHoliday = row.Date.Subtract(today).Days;
                                            string pluralDays = daysToHoliday == 1 ? "" : "s";
                                            string attributes = "";
                                            var day = row.Date.DayOfWeek;
                                            if ((day == DayOfWeek.Sunday) || (day == DayOfWeek.Saturday))
                                            {
                                                attributes = "(👍 Weekend)";
                                            }
                                            else if ((day == DayOfWeek.Friday) || (day == DayOfWeek.Monday))
                                            {
                                                attributes = "(👍 Adjacent to weekend)";
                                            }
                                        
                                            return MessageFieldElement.MessageField(
                                                row.Name,
                                                $"{row.Date.ToShortDateString()} ({row.Date.DayOfWeek}, in {daysToHoliday} day{pluralDays}) {attributes}"
                                            );
                                        })
                                        .ToList<MessageFieldElement>()
                                    )
                            })
                    }));
        }
        else
        {
            await chatClient.Messages.SendMessageAsync(
                recipient: MessageRecipient.Member(ProfileIdentifier.Id(payload.UserId)),
                content: ChatMessage.Block(
                    outline: new MessageOutline(reply, new ApiIcon("smile")),
                    sections: new List<MessageSectionElement>()));
        }
    }
    
    private async Task HandleHelpAsync(MessagePayload payload, ChatClient chatClient)
    {
        var commands = GetCommands();

        await chatClient.Messages.SendMessageAsync(
            recipient: MessageRecipient.Member(ProfileIdentifier.Id(payload.UserId)),
            content: ChatMessage.Block(
                outline: new MessageOutline("Holiday bot help", new ApiIcon("smile")),
                sections: new List<MessageSectionElement>
                {
                    MessageSectionElement.MessageSection(
                        header: $"List of available commands",
                        elements: new List<MessageBlockElement>
                        {
                            MessageBlockElement.MessageFields(
                                commands.CommandsItems
                                    .Select(it => MessageFieldElement.MessageField(it.Name, it.Description))
                                    .ToList<MessageFieldElement>())
                        })
                }));
    }
    
    private async Task HandleStatusAsync(MessagePayload payload, ChatClient chatClient)
    {
        var config = DGSEndpointConfig.LoadConfig();
        var allEntries = HolidayData.GetHolidayEntries();
        
        await chatClient.Messages.SendMessageAsync(
            recipient: MessageRecipient.Member(ProfileIdentifier.Id(payload.UserId)),
            content: ChatMessage.Block(
                outline: new MessageOutline($"Status (Space)"),
                sections: new List<MessageSectionElement>
                {
                    MessageSectionElement.MessageSection(
                        header: "Registered resources",
                        elements: new List<MessageBlockElement>
                        {
                            MessageBlockElement.MessageFields(
                                config.DGSResourceIdSet
                                    .Select(row => MessageFieldElement.MessageField(row, config.EndpointBaseAddress + config.EndpointRequestUri + row))
                                    .ToList<MessageFieldElement>())
                        }),
                    MessageSectionElement.MessageSection(
                        header: "Entries",
                        elements: new List<MessageBlockElement>
                        {
                            MessageBlockElement.MessageFields(
                                allEntries
                                    .Select(row => MessageFieldElement.MessageField(row.Name, $"{row.Date.ToShortDateString()} ({row.Date.DayOfWeek})"))
                                    .ToList<MessageFieldElement>())
                        }),
                }));
    }
    
    private static Commands GetCommands()
    {
        return new Commands(new List<CommandDetail>
        {
            new CommandDetail("help", "Show this help"),
            new CommandDetail("next", "Get the date of the next local holiday"),
            new CommandDetail("status", "Checks the system status and holiday definitions"),
        });
    }
}