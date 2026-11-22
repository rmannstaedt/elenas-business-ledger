using System.Globalization;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.ViewModels;

// Holds the dashboard row's editable time and plan alongside its saved identity and billing state.
public sealed class LessonDisplayModel : ViewModelBase
{
    private const int FirstTimeOptionHour = 8;
    private const int LastTimeOptionHour = 20;
    private const int TimeOptionIntervalMinutes = 15;
    public const string NoTimeOptionText = "No time";

    private static readonly IReadOnlyList<string> TimeOptionValues =
    [
        NoTimeOptionText,
        .. Enumerable
            .Range(0, (((LastTimeOptionHour - FirstTimeOptionHour) * 60) / TimeOptionIntervalMinutes) + 1)
            .Select(index => new TimeOnly(FirstTimeOptionHour, 0)
                .AddMinutes(index * TimeOptionIntervalMinutes)
                .ToString("HH\\:mm", CultureInfo.InvariantCulture))
    ];

    private TimeZoneInfo lessonTimeZone;
    private string id;
    private DateTimeOffset? savedStartsAt;
    private bool savedHasKnownStartTime = true;
    private DateTime? startsOnDate;
    private string startsAtTimeText;
    private LessonState state;
    private PaymentStatus? paymentStatus;
    private bool isDraft;
    private int planCatalogId;
    private string planName = string.Empty;

    public LessonDisplayModel(DashboardLesson lesson, TimeZoneInfo? lessonTimeZone = null)
    {
        id = lesson.Id;
        StudentShortName = lesson.StudentShortName;
        StudentId = lesson.StudentId;
        NeedsCalendarReview = lesson.NeedsCalendarReview;
        NeedsPlan = lesson.NeedsPlan;
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
        startsOnDate = lesson.StartsAt.Date;
        startsAtTimeText = lesson.HasKnownStartTime
            ? lesson.StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture)
            : string.Empty;
        savedStartsAt = lesson.StartsAt;
        savedHasKnownStartTime = lesson.HasKnownStartTime;
        state = lesson.State;
        IsInvoiced = lesson.IsInvoiced;
        InvoiceNumber = lesson.InvoiceNumber;
        paymentStatus = lesson.PaymentStatus;
        LocalLessonId = lesson.LocalLessonId;
        planCatalogId = lesson.PlanCatalogId;
        planName = lesson.PlanName;
    }

    private LessonDisplayModel(
        string studentShortName,
        TimeZoneInfo lessonTimeZone,
        DateTime? startsOnDate,
        TimeOnly? startsAt,
        DashboardPlanOption? plan,
        int? studentId)
    {
        id = $"draft-{Guid.NewGuid():N}";
        StudentShortName = studentShortName;
        StudentId = studentId;
        this.lessonTimeZone = lessonTimeZone;
        this.startsOnDate = startsOnDate;
        startsAtTimeText = startsAt?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
        planCatalogId = plan?.PlanCatalogId ?? 0;
        planName = plan?.Name ?? string.Empty;
        state = LessonState.Planned;
        IsInvoiced = false;
        InvoiceNumber = null;
        paymentStatus = null;
        isDraft = true;
    }

    public static LessonDisplayModel CreateDraft(
        string studentShortName,
        TimeZoneInfo lessonTimeZone,
        DateTime? startsOnDate = null,
        TimeOnly? startsAt = null,
        DashboardPlanOption? plan = null,
        int? studentId = null) =>
        new(studentShortName, lessonTimeZone, startsOnDate, startsAt, plan, studentId);

    public string Id => id;

    public string StudentShortName { get; }

    public int? StudentId { get; }

    public int LocalLessonId { get; private set; }

    public DateTimeOffset StartsAt
    {
        get
        {
            return TryGetStartsAt(out var startsAt)
                ? startsAt
                : DateTimeOffset.MinValue;
        }
    }

    public DateTime? StartsOnDate
    {
        get => startsOnDate;
        set
        {
            if (SetProperty(ref startsOnDate, value))
            {
                OnPropertyChanged(nameof(StartsAt));
                OnPropertyChanged(nameof(StartsAtText));
                OnPropertyChanged(nameof(StartsOnDateText));
                OnPropertyChanged(nameof(HasDateTimeChange));
            }
        }
    }

    public string StartsAtTimeText
    {
        get => startsAtTimeText;
        set
        {
            if (SetProperty(ref startsAtTimeText, value))
            {
                OnPropertyChanged(nameof(StartsAt));
                OnPropertyChanged(nameof(StartsAtText));
                OnPropertyChanged(nameof(HasKnownStartTime));
                OnPropertyChanged(nameof(HasDateTimeChange));
            }
        }
    }

    public IReadOnlyList<string> TimeOptions => TimeOptionValues;

    public LessonState State
    {
        get => state;
        set
        {
            if (SetProperty(ref state, value))
            {
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanEditPlan));
                OnPropertyChanged(nameof(CanToggleState));
                OnPropertyChanged(nameof(StateToggleToolTip));
            }
        }
    }

    public bool IsDraft
    {
        get => isDraft;
        private set
        {
            if (SetProperty(ref isDraft, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanEditPlan));
                OnPropertyChanged(nameof(CanToggleState));
                OnPropertyChanged(nameof(HasDateTimeChange));
            }
        }
    }

    public bool IsInvoiced { get; }

    public InvoiceNumber? InvoiceNumber { get; }

    public bool NeedsCalendarReview { get; }

    public bool NeedsPlan { get; private set; }

    public bool CanEdit => (IsDraft || State == LessonState.Planned) && !IsInvoiced && !NeedsCalendarReview && !NeedsPlan;

    public bool CanEditPlan => NeedsPlan && !IsInvoiced || CanEdit && (IsDraft || LocalLessonId > 0);

    public bool CanToggleState => !IsDraft && !IsInvoiced && !NeedsPlan;

    public bool HasDateTimeChange => HasDateOrTimeChange();
    public bool HasKnownStartTime => !string.IsNullOrWhiteSpace(StartsAtTimeText);

    public PaymentStatus? PaymentStatus
    {
        get => paymentStatus;
        set
        {
            if (SetProperty(ref paymentStatus, value))
            {
                OnPropertyChanged(nameof(PaymentStatusText));
            }
        }
    }

    public string StartsAtText => TryGetStartsAt(out var startsAt)
        ? HasKnownStartTime
            ? startsAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
            : startsAt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
        : string.Empty;

    public string StartsOnDateText => StartsOnDate is { } date
        ? date.ToString("dddd dd-MM", CultureInfo.InvariantCulture)
        : string.Empty;

    public string StateText => NeedsCalendarReview ? "Review needed" : NeedsPlan ? "Plan needed" : State == LessonState.Completed ? "Completed" : "Planned";

    public int PlanCatalogId
    {
        get => planCatalogId;
        set => SetProperty(ref planCatalogId, value);
    }

    public string PlanName
    {
        get => planName;
        private set => SetProperty(ref planName, value);
    }

    public string StateToggleToolTip => NeedsCalendarReview ? "Review pending calendar change" : State == LessonState.Completed
        ? "Mark lesson as planned"
        : "Mark lesson as completed";

    public string InvoicedText => IsInvoiced ? "Yes" : "No";

    public string PaymentStatusText => PaymentStatus switch
    {
        Core.Invoices.PaymentStatus.Paid => "Paid",
        Core.Invoices.PaymentStatus.Outstanding => "Outstanding",
        _ => string.Empty
    };

    // Makes the saved date and time the new baseline for detecting further edits.
    public void AcceptDateTimeChange()
    {
        savedStartsAt = StartsAt;
        savedHasKnownStartTime = HasKnownStartTime;
    }

    // Replaces the draft identity with the created calendar event so future edits target the saved appointment.
    public void AcceptCreatedLesson(CalendarLesson lesson)
    {
        id = lesson.Id;
        startsOnDate = lesson.StartsAt.Date;
        startsAtTimeText = lesson.HasKnownStartTime
            ? lesson.StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture)
            : string.Empty;
        savedStartsAt = lesson.StartsAt;
        savedHasKnownStartTime = lesson.HasKnownStartTime;
        IsDraft = false;
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(CanEditPlan));
        OnPropertyChanged(nameof(StartsAt));
        OnPropertyChanged(nameof(StartsAtText));
        OnPropertyChanged(nameof(StartsOnDateText));
        OnPropertyChanged(nameof(StartsOnDate));
        OnPropertyChanged(nameof(StartsAtTimeText));
    }

    // Updates the row from the saved plan and attaches its local lesson ID when the appointment was just linked.
    public void AcceptPlanChange(LessonRecord lesson)
    {
        if (NeedsPlan)
        {
            LocalLessonId = lesson.Id;
            NeedsPlan = false;
            OnPropertyChanged(nameof(NeedsPlan));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanEditPlan));
            OnPropertyChanged(nameof(CanToggleState));
            OnPropertyChanged(nameof(StateText));
        }
        planCatalogId = lesson.PlanCatalogId;
        PlanName = lesson.PlanName;
        OnPropertyChanged(nameof(PlanCatalogId));
    }

    public bool TryGetStartsAt(out DateTimeOffset startsAt)
    {
        return TryGetStartsAt(out startsAt, out _);
    }

    // Builds the appointment time in the lesson's time zone and distinguishes a blank time from an entered midnight.
    public bool TryGetStartsAt(out DateTimeOffset startsAt, out bool hasKnownStartTime)
    {
        startsAt = default;
        hasKnownStartTime = false;
        if (StartsOnDate is not { } date)
        {
            return false;
        }

        TimeOnly time;
        if (string.IsNullOrWhiteSpace(StartsAtTimeText))
        {
            time = TimeOnly.MinValue;
        }
        else if (TimeOnly.TryParseExact(StartsAtTimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
        {
            time = parsedTime;
            hasKnownStartTime = true;
        }
        else
        {
            return false;
        }

        var localDateTime = DateOnly.FromDateTime(date).ToDateTime(time);
        startsAt = new DateTimeOffset(localDateTime, lessonTimeZone.GetUtcOffset(localDateTime));
        return true;
    }

    // Compares the editor values with the saved time, including changes between timed and all-day lessons.
    public bool HasDateOrTimeChange() =>
        TryGetStartsAt(out var startsAt, out var hasKnownStartTime) &&
        (startsAt != savedStartsAt || hasKnownStartTime != savedHasKnownStartTime);
}
