namespace ElenasBusinessLedger.Core.Students;

// Pairs the stable student ID with its display label so a list selection survives a short-name change.
public sealed record StudentChoice(int StudentId, string ShortName)
{
    public override string ToString() => ShortName;
}

// Normalizes short names consistently for uniqueness checks and initial calendar matching.
public static class StudentShortNameKey
{
    public static string From(string shortName) => shortName.Trim().ToUpperInvariant();
}
