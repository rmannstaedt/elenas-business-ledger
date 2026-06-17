namespace MySmallBusinessLedger.App.Services;

public interface IUiClock
{
    DateTimeOffset Now { get; }
}
