using System.Globalization;

namespace SMS.Application.Common.Messaging;

/// <summary>
/// Builds the text for common parent-notification messages. Kept deliberately small and
/// SMS-friendly (short, plain text) for the Zimbabwe market.
/// </summary>
public static class MessageTemplates
{
    public static string FeeReminder(string studentName, decimal balance, string currency, string schoolName)
        => $"Dear Parent/Guardian, {studentName} has an outstanding fees balance of {currency} {balance.ToString("0.00", CultureInfo.InvariantCulture)}. Kindly settle at your earliest convenience. - {schoolName}";

    public static string ResultsPublished(string studentName, string termName, string schoolName)
        => $"Dear Parent/Guardian, the {termName} results for {studentName} have been published. Please log in to view the report card. - {schoolName}";

    public static string AbsenceAlert(string studentName, DateTime date, string schoolName)
        => $"Dear Parent/Guardian, {studentName} was marked absent on {date:dd MMM yyyy}. Please contact the school if this is unexpected. - {schoolName}";
}
