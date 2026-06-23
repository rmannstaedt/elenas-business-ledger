using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteCompanyProfileRepository : ICompanyProfileRepository
{
    private const int SingletonId = 1;
    private readonly Func<LedgerDbContext> createContext;

    public SqliteCompanyProfileRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<CompanyProfile> GetAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.CompanyProfiles.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException(
                "Company profile is not configured. Open Application > Settings before writing invoices.");
        }

        return ToDomain(entity);
    }

    public async Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.CompanyProfiles.SingleOrDefaultAsync(
            storedProfile => storedProfile.Id == SingletonId,
            cancellationToken);

        if (entity is null)
        {
            entity = new CompanyProfileEntity { Id = SingletonId };
            context.CompanyProfiles.Add(entity);
        }

        entity.Name = profile.Name;
        entity.Address = profile.Address;
        entity.Phone = profile.Phone;
        entity.Email = profile.Email;
        entity.Cvr = profile.Cvr;
        entity.VatRegistrationDate = profile.VatRegistrationDate;
        entity.VatStatus = profile.VatStatus;
        entity.InvoiceFolderPath = profile.InvoiceFolderPath;
        entity.ConfiguredInvoicePrinterName = profile.ConfiguredInvoicePrinterName;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static CompanyProfile ToDomain(CompanyProfileEntity entity) =>
        CompanyProfile.Create(
            entity.Name,
            entity.Address,
            entity.Phone,
            entity.Email,
            entity.Cvr,
            entity.VatRegistrationDate,
            entity.VatStatus,
            entity.InvoiceFolderPath,
            entity.ConfiguredInvoicePrinterName);
}
