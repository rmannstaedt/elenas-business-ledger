using ElenasBusinessLedger.Core.Time;

namespace ElenasBusinessLedger.App.Services;

public sealed class SystemUiClock : IUiClock, IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
