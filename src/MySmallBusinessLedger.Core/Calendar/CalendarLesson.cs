using MySmallBusinessLedger.Core.Lessons;

namespace MySmallBusinessLedger.Core.Calendar;

public sealed record CalendarLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State = LessonState.Planned);
