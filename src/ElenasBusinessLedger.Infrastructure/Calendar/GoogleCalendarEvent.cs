namespace ElenasBusinessLedger.Infrastructure.Calendar;

// Carries the Google event ID, summary, and start information that lesson matching needs from the API.
public sealed record GoogleCalendarEvent(
    string Id,
    string Summary,
    DateTimeOffset StartsAt,
    bool HasKnownStartTime = true);
