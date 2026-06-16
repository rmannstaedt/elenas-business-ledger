namespace ElenaSmallBusinessLedger.Core.Time;

public interface IClock
{
    DateTimeOffset Now { get; }
}
