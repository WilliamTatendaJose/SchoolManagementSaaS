using SMS.Application.Common.Messaging;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Messaging;

/// <summary>
/// SMS delivery channel; delegates to the configured <see cref="ISmsService"/> provider.
/// </summary>
public class SmsChannel : IMessageChannel
{
    private readonly ISmsService _smsService;

    public SmsChannel(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public string Channel => MessageChannels.Sms;

    public async Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default)
    {
        var sent = await _smsService.SendSmsAsync(recipient, content, cancellationToken);
        return sent ? MessageDeliveryResult.Ok() : MessageDeliveryResult.Fail("SMS provider did not accept the message");
    }
}
