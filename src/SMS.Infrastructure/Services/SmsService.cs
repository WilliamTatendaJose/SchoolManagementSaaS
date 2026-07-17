using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services;

/// <summary>
/// SMS service implementation (placeholder for actual SMS provider integration)
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly IApplicationDbContext _dbContext;

    public SmsService(ILogger<SmsService> logger, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate with actual SMS provider (e.g., Twilio, Africa's Talking, etc.)
        _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", phoneNumber, message);
        
        await Task.Delay(100, cancellationToken); // Simulate API call
        
        return true;
    }

    public async Task<int> SendBulkSmsAsync(IEnumerable<(string PhoneNumber, string Message)> messages, CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        
        foreach (var (phoneNumber, message) in messages)
        {
            if (await SendSmsAsync(phoneNumber, message, cancellationToken))
            {
                successCount++;
            }
        }
        
        return successCount;
    }

    public async Task<int> GetRemainingCreditsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.SmsCredits)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
