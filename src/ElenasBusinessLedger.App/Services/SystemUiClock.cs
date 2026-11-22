using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.App.Services;

// Supplies actual system time to WPF date handling and to Core workflows.
public sealed class SystemUiClock : IUiClock, IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
