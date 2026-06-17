using MySmallBusinessLedger.Core.Time;

namespace MySmallBusinessLedger.App.Services;

public sealed class SystemUiClock : IUiClock, IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
