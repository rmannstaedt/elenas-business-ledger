using ElenaSmallBusinessLedger.Core.Time;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SystemUiClock : IUiClock, IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
