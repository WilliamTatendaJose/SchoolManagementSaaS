namespace SMS.Application.Common.Finance;

public static class StudentAccountTransactionTypes
{
    /// <summary>Cash overpayment the cashier chose to credit instead of handing back as change.</summary>
    public const string OverpaymentCredit = "OverpaymentCredit";

    /// <summary>Account balance drawn down to pay an invoice.</summary>
    public const string AppliedToInvoice = "AppliedToInvoice";
}
