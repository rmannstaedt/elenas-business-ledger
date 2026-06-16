namespace ElenaSmallBusinessLedger.Core.Calendar;

public sealed record CalendarLessonEntry(string StudentShortName, DateTimeOffset StartsAt)
{
    public static bool TryParse(string text, DateTimeOffset startsAt, out CalendarLessonEntry? entry)
    {
        entry = null;

        if (string.IsNullOrEmpty(text) || text[0] != '$')
        {
            return false;
        }

        var firstLineEnd = text.IndexOfAny(['\r', '\n']);
        var firstLine = firstLineEnd >= 0 ? text[..firstLineEnd] : text;
        var shortName = firstLine[1..].TrimStart();

        if (shortName.Length == 0)
        {
            return false;
        }

        entry = new CalendarLessonEntry(shortName, startsAt);
        return true;
    }
}
