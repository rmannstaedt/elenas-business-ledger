namespace ElenaSmallBusinessLedger.App.Services;

public interface IUiClock
{
    DateTimeOffset Now { get; }
}
