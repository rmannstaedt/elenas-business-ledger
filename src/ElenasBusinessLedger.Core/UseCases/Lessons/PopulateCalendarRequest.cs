namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed record PopulateCalendarRequest(
    string StudentShortName,
    DateOnly DisplayMonth);
