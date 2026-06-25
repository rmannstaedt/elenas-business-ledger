namespace MySmallBusinessLedger.Core.Invoices;

public sealed record CompanySnapshot(
    string Name,
    string Address,
    string? Cvr,
    string PaymentInstructions = "",
    string BankRegistrationNumber = "",
    string BankAccountNumber = "",
    string MobilePayAccount = "");
