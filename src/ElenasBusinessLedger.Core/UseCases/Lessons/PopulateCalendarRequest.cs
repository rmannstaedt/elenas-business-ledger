namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Identifies whose recurring schedule should populate the selected calendar month.
public sealed record PopulateCalendarRequest(
    string StudentShortName,
    DateOnly DisplayMonth,
    int? StudentId = null);
