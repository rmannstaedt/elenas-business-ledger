namespace ElenasBusinessLedger.App.Services;

// Gives view models a replaceable source of time for dates, defaults, and refresh timing.
public interface IUiClock
{
    DateTimeOffset Now { get; }
}
