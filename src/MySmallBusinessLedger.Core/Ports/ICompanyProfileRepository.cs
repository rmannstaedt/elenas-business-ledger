using MySmallBusinessLedger.Core.Company;

namespace MySmallBusinessLedger.Core.Ports;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken);
}
