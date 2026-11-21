namespace ElenasBusinessLedger.Core.Company;

// Holds the business, VAT, invoice, and printer settings used to prepare and issue invoices.
public sealed record CompanyProfile(
    string Name,
    string Address,
    string Phone,
    string Email,
    string? Cvr,
    DateOnly? VatRegistrationDate,
    VatStatus VatStatus,
    string InvoiceFolderPath,
    string ConfiguredInvoicePrinterName = "",
    string PaymentInstructions = "",
    string BankRegistrationNumber = "",
    string BankAccountNumber = "",
    string MobilePayAccount = "")
{
    public static CompanyProfile Create(
        string name,
        string address,
        string phone,
        string email,
        string? cvr,
        DateOnly? vatRegistrationDate,
        VatStatus vatStatus,
        string invoiceFolderPath,
        string configuredInvoicePrinterName = "",
        string paymentInstructions = "",
        string bankRegistrationNumber = "",
        string bankAccountNumber = "",
        string mobilePayAccount = "") =>
        new(
            name,
            address,
            phone,
            email,
            cvr,
            vatRegistrationDate,
            vatStatus,
            invoiceFolderPath,
            configuredInvoicePrinterName,
            paymentInstructions,
            bankRegistrationNumber,
            bankAccountNumber,
            mobilePayAccount);

    // Checks the VAT registration date used by billing to decide whether a period is taxable.
    public bool ChargesVatOn(DateOnly invoiceDate) =>
        VatStatus == VatStatus.Registered &&
        VatRegistrationDate is not null &&
        invoiceDate >= VatRegistrationDate.Value;
}
