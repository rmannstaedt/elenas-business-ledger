using ElenaSmallBusinessLedger.Core.Lessons;

namespace ElenaSmallBusinessLedger.Core.Calendar;

public sealed record CalendarLessonEntry(string StudentShortName, DateTimeOffset StartsAt, LessonState State = LessonState.Planned)
{
    private const string CompletedStatusLine = "Status: Completed";

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

        var state = ParseState(text);
        entry = new CalendarLessonEntry(shortName, startsAt, state);
        return true;
    }

    public static string WithState(string text, LessonState state)
    {
        var lines = SplitLines(text)
            .Where(line => !IsStatusLine(line))
            .ToList();

        if (state == LessonState.Completed)
        {
            lines.Add(CompletedStatusLine);
        }

        return string.Join('\n', lines);
    }

    private static LessonState ParseState(string text) =>
        SplitLines(text)
            .Skip(1)
            .Any(line => string.Equals(line.Trim(), CompletedStatusLine, StringComparison.OrdinalIgnoreCase))
            ? LessonState.Completed
            : LessonState.Planned;

    private static bool IsStatusLine(string line) =>
        string.Equals(line.Trim(), CompletedStatusLine, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
}
