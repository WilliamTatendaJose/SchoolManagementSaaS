namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for sending SMS messages
/// </summary>
public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    Task<int> SendBulkSmsAsync(IEnumerable<(string PhoneNumber, string Message)> messages, CancellationToken cancellationToken = default);
    Task<int> GetRemainingCreditsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
