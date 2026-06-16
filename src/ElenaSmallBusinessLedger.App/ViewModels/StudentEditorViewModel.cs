using System.Collections.ObjectModel;
using System.Globalization;
using ElenaSmallBusinessLedger.App.Commands;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Core.UseCases.Students;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class StudentEditorViewModel : ViewModelBase
{
    private const string DateFormat = "dd-MM-yyyy";
    private readonly IStudentEditorService studentEditorService;
    private string shortName = string.Empty;
    private string fullName = string.Empty;
    private string address = string.Empty;
    private string phone = string.Empty;
    private string email = string.Empty;
    private AgeBracket ageBracket = AgeBracket.Standard;
    private string birthdateText = string.Empty;
    private bool isActive = true;
    private string planName = "Standard";
    private string planPriceWithoutVatText = string.Empty;
    private LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One;
    private string planDurationMinutesText = "60";
    private PlanFlexibility planFlexibility = PlanFlexibility.MoveOrCancelByAgreement;
    private string planNumberOfStudentsText = "1";
    private string planEffectiveDateText = DateOnly.FromDateTime(DateTime.Today).ToString(DateFormat, CultureInfo.InvariantCulture);
    private string planStartsOnText = DateOnly.FromDateTime(DateTime.Today).ToString(DateFormat, CultureInfo.InvariantCulture);
    private string planEndsOnText = string.Empty;
    private DayOfWeek newScheduleWeekday = DayOfWeek.Tuesday;
    private string newScheduleTimeText = "15:00";
    private string? errorMessage;
    private bool isBusy;

    private StudentEditorViewModel(IStudentEditorService studentEditorService)
    {
        this.studentEditorService = studentEditorService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        AddScheduleCommand = new AsyncRelayCommand(AddScheduleAsync, () => !IsBusy);
        RemoveScheduleCommand = new AsyncRelayCommand(
            parameter =>
            {
                if (parameter is ScheduleEntryDisplayModel schedule)
                {
                    Schedule.Remove(schedule);
                }

                return Task.CompletedTask;
            },
            parameter => parameter is ScheduleEntryDisplayModel && !IsBusy);
    }

    public event EventHandler<bool?>? CloseRequested;

    public static StudentEditorViewModel CreateNew(IStudentEditorService studentEditorService) =>
        new(studentEditorService);

    public static StudentEditorViewModel FromSnapshot(
        IStudentEditorService studentEditorService,
        StudentEditorSnapshot snapshot)
    {
        var viewModel = new StudentEditorViewModel(studentEditorService)
        {
            ShortName = snapshot.ShortName,
            FullName = snapshot.FullName,
            Address = snapshot.Address,
            Phone = snapshot.Phone,
            Email = snapshot.Email,
            AgeBracket = snapshot.AgeBracket,
            BirthdateText = snapshot.Birthdate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty,
            IsActive = snapshot.IsActive
        };

        var currentPlan = snapshot.PlanPeriods
            .OrderByDescending(plan => plan.StartsOn)
            .FirstOrDefault();
        if (currentPlan is not null)
        {
            viewModel.PlanName = currentPlan.PlanName;
            viewModel.PlanPriceWithoutVatText = currentPlan.PriceWithoutVat.ToString(CultureInfo.InvariantCulture);
            viewModel.LessonsPerWeek = currentPlan.LessonsPerWeek;
            viewModel.PlanDurationMinutesText = currentPlan.DurationMinutes.ToString(CultureInfo.InvariantCulture);
            viewModel.PlanFlexibility = currentPlan.Flexibility;
            viewModel.PlanNumberOfStudentsText = currentPlan.NumberOfStudents.ToString(CultureInfo.InvariantCulture);
            viewModel.PlanEffectiveDateText = DateOnly.FromDateTime(currentPlan.PlanEffectiveFrom.Date).ToString(DateFormat, CultureInfo.InvariantCulture);
            viewModel.PlanStartsOnText = currentPlan.StartsOn.ToString(DateFormat, CultureInfo.InvariantCulture);
            viewModel.PlanEndsOnText = currentPlan.EndsOn?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        foreach (var schedule in snapshot.Schedule)
        {
            viewModel.Schedule.Add(new ScheduleEntryDisplayModel(schedule.Weekday, schedule.StartsAt));
        }

        return viewModel;
    }

    public IReadOnlyList<AgeBracket> AgeBracketOptions { get; } =
        [AgeBracket.Standard, AgeBracket.Young];

    public IReadOnlyList<LessonsPerWeek> LessonsPerWeekOptions { get; } =
        [LessonsPerWeek.One, LessonsPerWeek.Two, LessonsPerWeek.ByAppointment];

    public IReadOnlyList<PlanFlexibility> PlanFlexibilityOptions { get; } =
        [PlanFlexibility.MoveOrCancelByAgreement, PlanFlexibility.None];

    public IReadOnlyList<DayOfWeek> WeekdayOptions { get; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public ObservableCollection<ScheduleEntryDisplayModel> Schedule { get; } = [];

    public string ShortName
    {
        get => shortName;
        set => SetProperty(ref shortName, value);
    }

    public string FullName
    {
        get => fullName;
        set => SetProperty(ref fullName, value);
    }

    public string Address
    {
        get => address;
        set => SetProperty(ref address, value);
    }

    public string Phone
    {
        get => phone;
        set => SetProperty(ref phone, value);
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public AgeBracket AgeBracket
    {
        get => ageBracket;
        set => SetProperty(ref ageBracket, value);
    }

    public string BirthdateText
    {
        get => birthdateText;
        set => SetProperty(ref birthdateText, value);
    }

    public bool IsActive
    {
        get => isActive;
        set => SetProperty(ref isActive, value);
    }

    public string PlanName
    {
        get => planName;
        set => SetProperty(ref planName, value);
    }

    public string PlanPriceWithoutVatText
    {
        get => planPriceWithoutVatText;
        set => SetProperty(ref planPriceWithoutVatText, value);
    }

    public LessonsPerWeek LessonsPerWeek
    {
        get => lessonsPerWeek;
        set => SetProperty(ref lessonsPerWeek, value);
    }

    public string PlanDurationMinutesText
    {
        get => planDurationMinutesText;
        set => SetProperty(ref planDurationMinutesText, value);
    }

    public PlanFlexibility PlanFlexibility
    {
        get => planFlexibility;
        set => SetProperty(ref planFlexibility, value);
    }

    public string PlanNumberOfStudentsText
    {
        get => planNumberOfStudentsText;
        set => SetProperty(ref planNumberOfStudentsText, value);
    }

    public string PlanEffectiveDateText
    {
        get => planEffectiveDateText;
        set => SetProperty(ref planEffectiveDateText, value);
    }

    public string PlanStartsOnText
    {
        get => planStartsOnText;
        set => SetProperty(ref planStartsOnText, value);
    }

    public string PlanEndsOnText
    {
        get => planEndsOnText;
        set => SetProperty(ref planEndsOnText, value);
    }

    public DayOfWeek NewScheduleWeekday
    {
        get => newScheduleWeekday;
        set => SetProperty(ref newScheduleWeekday, value);
    }

    public string NewScheduleTimeText
    {
        get => newScheduleTimeText;
        set => SetProperty(ref newScheduleTimeText, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
                AddScheduleCommand.RaiseCanExecuteChanged();
                RemoveScheduleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool WasSaved { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand AddScheduleCommand { get; }

    public AsyncRelayCommand RemoveScheduleCommand { get; }

    private Task AddScheduleAsync()
    {
        if (!TimeOnly.TryParseExact(NewScheduleTimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startsAt))
        {
            ErrorMessage = "Schedule time must use HH:mm.";
            return Task.CompletedTask;
        }

        if (!Schedule.Any(entry => entry.Weekday == NewScheduleWeekday && entry.StartsAt == startsAt))
        {
            Schedule.Add(new ScheduleEntryDisplayModel(NewScheduleWeekday, startsAt));
        }

        ErrorMessage = null;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        var request = CreateRequest();
        if (request is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await studentEditorService.SaveAsync(request, CancellationToken.None);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Student could not be saved.";
                return;
            }

            WasSaved = true;
            ErrorMessage = null;
            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private SaveStudentRequest? CreateRequest()
    {
        if (!TryParseOptionalDate(BirthdateText, out var birthdate) ||
            !TryParseDate(PlanEffectiveDateText, "Plan effective date", out var effectiveDate) ||
            !TryParseDate(PlanStartsOnText, "Plan start date", out var startsOn) ||
            !TryParseOptionalDate(PlanEndsOnText, out var endsOn) ||
            !decimal.TryParse(PlanPriceWithoutVatText, NumberStyles.Number, CultureInfo.InvariantCulture, out var priceWithoutVat) ||
            !int.TryParse(PlanDurationMinutesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationMinutes) ||
            !int.TryParse(PlanNumberOfStudentsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numberOfStudents))
        {
            ErrorMessage ??= "Please check the plan fields.";
            return null;
        }

        ErrorMessage = null;
        return new SaveStudentRequest(
            ShortName,
            FullName,
            Address,
            Phone,
            Email,
            AgeBracket,
            birthdate,
            IsActive,
            [
                new StudentPlanPeriodInput(
                    PlanName,
                    priceWithoutVat,
                    LessonsPerWeek,
                    durationMinutes,
                    PlanFlexibility,
                    numberOfStudents,
                    new DateTimeOffset(effectiveDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                    startsOn,
                    endsOn)
            ],
            Schedule.Select(entry => new StudentScheduleInput(entry.Weekday, entry.StartsAt)).ToArray());
    }

    private bool TryParseDate(string text, string fieldName, out DateOnly date)
    {
        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        ErrorMessage = $"{fieldName} must use dd-MM-yyyy.";
        return false;
    }

    private bool TryParseOptionalDate(string text, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = parsed;
            return true;
        }

        ErrorMessage = "Dates must use dd-MM-yyyy.";
        return false;
    }
}
