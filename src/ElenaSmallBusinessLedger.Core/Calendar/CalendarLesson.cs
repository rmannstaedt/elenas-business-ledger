using ElenaSmallBusinessLedger.Core.Lessons;

namespace ElenaSmallBusinessLedger.Core.Calendar;

public sealed record CalendarLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State = LessonState.Planned);
