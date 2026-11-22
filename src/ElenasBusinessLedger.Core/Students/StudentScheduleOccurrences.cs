namespace ElenasBusinessLedger.Core.Students;

// Expands weekly student slots into dated occurrences for Calendar writes and dashboard navigation.
internal static class StudentScheduleOccurrences
{
    public static IEnumerable<DateTimeOffset> Between(
        IReadOnlyList<StudentScheduleEntry> schedule, DateOnly from, DateOnly to, TimeZoneInfo timeZone)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var entry in schedule)
            {
                if (entry.Weekday != date.DayOfWeek)
                {
                    continue;
                }

                var startsAt = date.ToDateTime(entry.StartsAt);
                yield return new DateTimeOffset(startsAt, timeZone.GetUtcOffset(startsAt));
            }
        }
    }
}
