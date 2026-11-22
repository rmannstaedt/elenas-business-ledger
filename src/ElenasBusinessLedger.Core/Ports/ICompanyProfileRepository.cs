using ElenasBusinessLedger.Core.Company;

namespace ElenasBusinessLedger.Core.Ports;

// Lets billing and Settings read or save the business profile through a storage-independent contract.
public interface ICompanyProfileRepository
{
    Task<CompanyProfile> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken);
}
