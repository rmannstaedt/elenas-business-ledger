namespace ElenasBusinessLedger.Core.Time;

// Gives time-dependent rules a clock that can be replaced in tests.
public interface IClock
{
    DateTimeOffset Now { get; }
}
