using ElenaSmallBusinessLedger.Core.Company;

namespace ElenaSmallBusinessLedger.Core.Ports;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile> GetAsync(CancellationToken cancellationToken);
}
