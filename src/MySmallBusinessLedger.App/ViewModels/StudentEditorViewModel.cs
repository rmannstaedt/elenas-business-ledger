using System.Collections.ObjectModel;
using System.Globalization;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.UseCases.Students;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class StudentEditorViewModel : ViewModelBase
{
    private readonly IStudentEditorService studentEditorService;
    private string shortName = string.Empty;
    private string fullName = string.Empty;
    private string address = string.Empty;
    private string phone = string.Empty;
    private string email = string.Empty;
    private AgeBracket ageBracket = AgeBracket.Standard;
    private DateTime? birthdateDate;
    private bool isActive = true;
    private string planName = "Standard";
    private decimal planPriceWithoutVat = 400m;
    private LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One;
    private int planDurationMinutes = 60;
    private PlanFlexibility planFlexibility = PlanFlexibility.MoveOrCancelByAgreement;
    private int planNumberOfStudents = 1;
    private DateTime planEffectiveDate = new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue);
    private DateTime planStartsOnDate = new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue);
    private DateTime? planEndsOnDate;
    private string? errorMessage;
    private bool isBusy;

    private StudentEditorViewModel(IStudentEditorService studentEditorService)
    {
        this.studentEditorService = studentEditorService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        AddPlanCommand = new AsyncRelayCommand(ShowAddPlanPlaceholderAsync, () => !IsBusy);
        RefreshSchedulePlacements();
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
            BirthdateDate = snapshot.Birthdate?.ToDateTime(TimeOnly.MinValue),
            IsActive = snapshot.IsActive
        };

        var currentPlan = snapshot.PlanPeriods
            .OrderByDescending(plan => plan.StartsOn)
            .FirstOrDefault();
        if (currentPlan is not null)
        {
            viewModel.PlanName = currentPlan.PlanName;
            viewModel.PlanPriceWithoutVat = currentPlan.PriceWithoutVat;
            viewModel.LessonsPerWeek = currentPlan.LessonsPerWeek;
            viewModel.PlanDurationMinutes = currentPlan.DurationMinutes;
            viewModel.PlanFlexibility = currentPlan.Flexibility;
            viewModel.PlanNumberOfStudents = currentPlan.NumberOfStudents;
            viewModel.PlanEffectiveDate = currentPlan.PlanEffectiveFrom.Date;
            viewModel.PlanStartsOnDate = currentPlan.StartsOn.ToDateTime(TimeOnly.MinValue);
            viewModel.PlanEndsOnDate = currentPlan.EndsOn?.ToDateTime(TimeOnly.MinValue);
        }

        viewModel.ApplySchedule(snapshot.Schedule);
        return viewModel;
    }

    public IReadOnlyList<AgeBracket> AgeBracketOptions { get; } =
        [AgeBracket.Standard, AgeBracket.Young];

    public IReadOnlyList<WeekdayOption> WeekdayOptions { get; } =
        [
            WeekdayOption.Unspecified,
            new("Monday", DayOfWeek.Monday),
            new("Tuesday", DayOfWeek.Tuesday),
            new("Wednesday", DayOfWeek.Wednesday),
            new("Thursday", DayOfWeek.Thursday),
            new("Friday", DayOfWeek.Friday),
            new("Saturday", DayOfWeek.Saturday),
            new("Sunday", DayOfWeek.Sunday)
        ];

    public ObservableCollection<SchedulePlacementViewModel> SchedulePlacements { get; } = [];

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

    public DateTime? BirthdateDate
    {
        get => birthdateDate;
        set => SetProperty(ref birthdateDate, value);
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

    public decimal PlanPriceWithoutVat
    {
        get => planPriceWithoutVat;
        set
        {
            if (SetProperty(ref planPriceWithoutVat, value))
            {
                OnPropertyChanged(nameof(PlanPriceWithoutVatText));
            }
        }
    }

    public string PlanPriceWithoutVatText => PlanPriceWithoutVat.ToString("0.##", CultureInfo.InvariantCulture);

    public LessonsPerWeek LessonsPerWeek
    {
        get => lessonsPerWeek;
        set
        {
            if (SetProperty(ref lessonsPerWeek, value))
            {
                OnPropertyChanged(nameof(LessonsPerWeekText));
                RefreshSchedulePlacements();
            }
        }
    }

    public string LessonsPerWeekText => LessonsPerWeek switch
    {
        LessonsPerWeek.One => "One lesson per week",
        LessonsPerWeek.Two => "Two lessons per week",
        LessonsPerWeek.ByAppointment => "By appointment",
        _ => LessonsPerWeek.ToString()
    };

    public int PlanDurationMinutes
    {
        get => planDurationMinutes;
        set
        {
            if (SetProperty(ref planDurationMinutes, value))
            {
                OnPropertyChanged(nameof(PlanDurationMinutesText));
            }
        }
    }

    public string PlanDurationMinutesText => PlanDurationMinutes.ToString(CultureInfo.InvariantCulture);

    public PlanFlexibility PlanFlexibility
    {
        get => planFlexibility;
        set
        {
            if (SetProperty(ref planFlexibility, value))
            {
                OnPropertyChanged(nameof(PlanFlexibilityText));
            }
        }
    }

    public string PlanFlexibilityText => PlanFlexibility switch
    {
        PlanFlexibility.None => "No moving or cancellation agreement",
        PlanFlexibility.MoveOrCancelByAgreement => "Move or cancel by agreement",
        _ => PlanFlexibility.ToString()
    };

    public int PlanNumberOfStudents
    {
        get => planNumberOfStudents;
        set
        {
            if (SetProperty(ref planNumberOfStudents, value))
            {
                OnPropertyChanged(nameof(PlanNumberOfStudentsText));
            }
        }
    }

    public string PlanNumberOfStudentsText => PlanNumberOfStudents.ToString(CultureInfo.InvariantCulture);

    public DateTime PlanEffectiveDate
    {
        get => planEffectiveDate;
        set => SetProperty(ref planEffectiveDate, value);
    }

    public DateTime PlanStartsOnDate
    {
        get => planStartsOnDate;
        set => SetProperty(ref planStartsOnDate, value);
    }

    public DateTime? PlanEndsOnDate
    {
        get => planEndsOnDate;
        set => SetProperty(ref planEndsOnDate, value);
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
                AddPlanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool WasSaved { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand AddPlanCommand { get; }

    private Task ShowAddPlanPlaceholderAsync()
    {
        ErrorMessage = "Adding a new plan will be implemented with the plan administration workflow.";
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
        var schedule = new List<StudentScheduleInput>();
        foreach (var placement in SchedulePlacements)
        {
            if (placement.SelectedWeekday.Weekday is null)
            {
                continue;
            }

            if (!TimeOnly.TryParseExact(placement.TimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startsAt))
            {
                ErrorMessage = $"{placement.Label} time must use HH:mm.";
                return null;
            }

            schedule.Add(new StudentScheduleInput(placement.SelectedWeekday.Weekday.Value, startsAt));
        }

        ErrorMessage = null;
        return new SaveStudentRequest(
            ShortName,
            FullName,
            Address,
            Phone,
            Email,
            AgeBracket,
            BirthdateDate is null ? null : DateOnly.FromDateTime(BirthdateDate.Value),
            IsActive,
            [
                new StudentPlanPeriodInput(
                    PlanName,
                    PlanPriceWithoutVat,
                    LessonsPerWeek,
                    PlanDurationMinutes,
                    PlanFlexibility,
                    PlanNumberOfStudents,
                    new DateTimeOffset(PlanEffectiveDate.Date, TimeSpan.Zero),
                    DateOnly.FromDateTime(PlanStartsOnDate),
                    PlanEndsOnDate is null ? null : DateOnly.FromDateTime(PlanEndsOnDate.Value))
            ],
            schedule);
    }

    private void ApplySchedule(IReadOnlyList<StudentScheduleInput> schedule)
    {
        RefreshSchedulePlacements();
        for (var index = 0; index < SchedulePlacements.Count; index++)
        {
            if (index >= schedule.Count)
            {
                break;
            }

            var entry = schedule[index];
            SchedulePlacements[index].SelectedWeekday =
                WeekdayOptions.First(option => option.Weekday == entry.Weekday);
            SchedulePlacements[index].TimeText = entry.StartsAt.ToString("HH\\:mm", CultureInfo.InvariantCulture);
        }
    }

    private void RefreshSchedulePlacements()
    {
        var placementCount = LessonsPerWeek switch
        {
            LessonsPerWeek.One => 1,
            LessonsPerWeek.Two => 2,
            _ => 0
        };

        while (SchedulePlacements.Count > placementCount)
        {
            SchedulePlacements.RemoveAt(SchedulePlacements.Count - 1);
        }

        while (SchedulePlacements.Count < placementCount)
        {
            SchedulePlacements.Add(new SchedulePlacementViewModel(
                SchedulePlacements.Count == 0 ? "Lesson 1" : "Lesson 2",
                WeekdayOptions));
        }
    }
}
