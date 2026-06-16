namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SystemUiClock : IUiClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
