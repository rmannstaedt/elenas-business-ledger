using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Company;

namespace ElenasBusinessLedger.Core.Ports;

// Commits a Settings submission atomically and returns the resulting Economy settings.
public interface IAppSettingsWriter
{
    Task<EconomySettings?> SaveAsync(
        CompanyProfile company,
        CalendarSettings calendar,
        EconomySettings? economy,
        CancellationToken cancellationToken);
}
