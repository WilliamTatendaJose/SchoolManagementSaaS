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

    public static string DisciplineNotice(string studentName, string incidentType, DateTime date, string schoolName)
        => $"Dear Parent/Guardian, a discipline incident ({incidentType}) was recorded for {studentName} on {date:dd MMM yyyy}. Please contact the school to discuss. - {schoolName}";

    public static string WeekendLeaveApproved(string studentName, DateTime departureDate, DateTime returnDate, string schoolName)
        => $"Dear Parent/Guardian, weekend leave for {studentName} has been approved from {departureDate:dd MMM yyyy} to {returnDate:dd MMM yyyy}. - {schoolName}";

    public static string AssignmentReminder(string studentName, string assignmentTitle, string subjectName, DateTime dueDate, string schoolName)
        => $"Dear Parent/Guardian, {studentName} has not yet submitted the {subjectName} assignment \"{assignmentTitle}\" due {dueDate:dd MMM yyyy}. Kindly ensure it is completed. - {schoolName}";

    /// <summary>
    /// Ordered body parameters for WhatsApp template sends (see
    /// <c>IMessageChannel.SendTemplateAsync</c>). These fill a Meta-approved template's
    /// {{1}}, {{2}}, ... placeholders in order - the exact wording lives in Meta's
    /// dashboard, not here, so a school's actually-approved template must be authored
    /// with this same parameter order and count for the substitution to make sense.
    /// </summary>
    public static class TemplateParams
    {
        public static string[] FeeReminder(string studentName, decimal balance, string currency)
            => [studentName, $"{currency} {balance.ToString("0.00", CultureInfo.InvariantCulture)}"];

        public static string[] DisciplineNotice(string studentName, string incidentType, DateTime date)
            => [studentName, incidentType, date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)];

        public static string[] AssignmentReminder(string studentName, string subjectName, string assignmentTitle, DateTime dueDate)
            => [studentName, subjectName, assignmentTitle, dueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)];
    }
}
