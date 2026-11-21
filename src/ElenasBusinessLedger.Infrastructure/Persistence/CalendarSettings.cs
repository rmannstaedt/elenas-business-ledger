namespace ElenasBusinessLedger.Infrastructure.Persistence;

// Pairs the selected calendar ID with the Google account used to connect to it.
public sealed record CalendarSettings(string SelectedCalendarId, string GoogleAccountEmail);
