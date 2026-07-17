namespace SMS.Application.Common.Messaging;

/// <summary>Supported message channel identifiers.</summary>
public static class MessageChannels
{
    public const string Sms = "SMS";
    public const string WhatsApp = "WhatsApp";
}

/// <summary>Message lifecycle statuses (stored on <c>Message.Status</c>).</summary>
public static class MessageStatuses
{
    public const string Draft = "Draft";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

/// <summary>Per-recipient delivery statuses (stored on <c>MessageRecipient.Status</c>).</summary>
public static class RecipientStatuses
{
    public const string Pending = "Pending";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}

/// <summary>Message categories (stored on <c>Message.MessageType</c>).</summary>
public static class MessageTypes
{
    public const string Announcement = "Announcement";
    public const string FeeReminder = "FeeReminder";
    public const string ResultsPublished = "ResultsPublished";
    public const string AbsenceAlert = "AbsenceAlert";
    public const string Discipline = "Discipline";
}
