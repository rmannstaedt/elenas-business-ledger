namespace ElenasBusinessLedger.App.Services;

public interface IUiClock
{
    DateTimeOffset Now { get; }
}
