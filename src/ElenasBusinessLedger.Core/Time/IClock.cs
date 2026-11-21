namespace ElenasBusinessLedger.Core.Time;

public interface IClock
{
    DateTimeOffset Now { get; }
}
