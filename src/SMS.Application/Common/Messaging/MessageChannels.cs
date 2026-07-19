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
    public const string Queued = "Queued";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

/// <summary>Per-recipient delivery statuses (stored on <c>MessageRecipient.Status</c>).</summary>
public static class RecipientStatuses
{
    public const string Pending = "Pending";
    /// <summary>Accepted by the provider; awaiting an async delivery receipt (WhatsApp).</summary>
    public const string Sent = "Sent";
    public const string Delivered = "Delivered";
    /// <summary>Delivered and opened by the recipient (WhatsApp read receipts).</summary>
    public const string Read = "Read";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";

    // How far a status has progressed along the delivery lifecycle. Used so out-of-order
    // webhook events can only advance a recipient forward, never regress it.
    private static readonly Dictionary<string, int> Rank = new()
    {
        [Pending] = 0,
        [Sent] = 1,
        [Delivered] = 2,
        [Read] = 3,
        [Failed] = 2,   // a terminal outcome level with Delivered
        [Skipped] = 2,
    };

    public static int RankOf(string status) => Rank.GetValueOrDefault(status, 0);

    /// <summary>A recipient the provider has confirmed reached the handset.</summary>
    public static bool IsConfirmedDelivered(string status) => status is Delivered or Read;
}

/// <summary>Message categories (stored on <c>Message.MessageType</c>).</summary>
public static class MessageTypes
{
    public const string Announcement = "Announcement";
    public const string FeeReminder = "FeeReminder";
    public const string ResultsPublished = "ResultsPublished";
    public const string AbsenceAlert = "AbsenceAlert";
    public const string Discipline = "Discipline";
    public const string BoardingLeave = "BoardingLeave";
    public const string AssignmentReminder = "AssignmentReminder";
}
