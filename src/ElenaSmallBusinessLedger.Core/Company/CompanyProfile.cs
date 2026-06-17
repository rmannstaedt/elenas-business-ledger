namespace MySmallBusinessLedger.Core.Company;

public sealed record CompanyProfile(
    string Name,
    string Address,
    string Phone,
    string Email,
    string? Cvr,
    DateOnly? VatRegistrationDate,
    VatStatus VatStatus,
    string InvoiceFolderPath)
{
    public static CompanyProfile Create(
        string name,
        string address,
        string phone,
        string email,
        string? cvr,
        DateOnly? vatRegistrationDate,
        VatStatus vatStatus,
        string invoiceFolderPath) =>
        new(name, address, phone, email, cvr, vatRegistrationDate, vatStatus, invoiceFolderPath);

    public bool ChargesVatOn(DateOnly invoiceDate) =>
        VatStatus == VatStatus.Registered &&
        VatRegistrationDate is not null &&
        invoiceDate >= VatRegistrationDate.Value;
}
