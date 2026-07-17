namespace SMS.Application.Features.Library;

/// <summary>Lifecycle statuses for a <c>BookLoan</c> (stored on <c>BookLoan.Status</c>).</summary>
public static class BookLoanStatuses
{
    public const string Borrowed = "Borrowed";
    public const string Returned = "Returned";
    public const string Lost = "Lost";
}
