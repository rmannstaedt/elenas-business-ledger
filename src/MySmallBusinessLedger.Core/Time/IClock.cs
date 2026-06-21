namespace MySmallBusinessLedger.Core.Time;

public interface IClock
{
    DateTimeOffset Now { get; }
}
