namespace SMS.Domain.Enums;

/// <summary>
/// Payment status options
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}
