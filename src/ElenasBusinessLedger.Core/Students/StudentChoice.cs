namespace ElenasBusinessLedger.Core.Students;

public sealed record StudentChoice(int StudentId, string ShortName)
{
    public override string ToString() => ShortName;
}

public static class StudentShortNameKey
{
    public static string From(string shortName) => shortName.Trim().ToUpperInvariant();
}
