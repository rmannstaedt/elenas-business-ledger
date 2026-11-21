namespace ElenasBusinessLedger.Core.Invoices;

// Copies the company's invoice details so later Settings changes do not alter the basis of a saved invoice.
public sealed record CompanySnapshot(
    string Name,
    string Address,
    string? Cvr,
    string PaymentInstructions = "",
    string BankRegistrationNumber = "",
    string BankAccountNumber = "",
    string MobilePayAccount = "");
