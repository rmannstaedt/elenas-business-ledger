using ElenasBusinessLedger.Core.Company;

namespace ElenasBusinessLedger.Core.Ports;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken);
}
