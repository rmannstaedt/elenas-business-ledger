using System.Collections.ObjectModel;
using System.Globalization;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class StudentEditorViewModel : ViewModelBase
{
    private readonly IStudentEditorService studentEditorService;
    private readonly bool requestCloseOnSave;
    private int studentId;
    private string shortName = string.Empty;
    private string fullName = string.Empty;
    private string address = string.Empty;
    private string addressLine2 = string.Empty;
    private string phone = string.Empty;
    private string email = string.Empty;
    private AgeBracket ageBracket = AgeBracket.Standard;
    private DateTime? birthdateDate;
    private bool isActive = true;
    private int planCatalogId;
    private string planName = "Standard";
    private decimal planPriceWithoutVat = 400m;
    private LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One;
    private int planDurationMinutes = 60;
    private PlanFlexibility planFlexibility = PlanFlexibility.MoveOrCancelByAgreement;
    private int planNumberOfStudents = 1;
    private DateTime planEffectiveDate = new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue);
    private string planPaymentTerms = string.Empty;
    private DateTime planStartsOnDate = new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue);
    private DateTime? planEndsOnDate;
    private PlanCatalogItem? selectedAssignablePlan;
    private bool requiresCatalogPlanAssignment;
    private string savedChangeStamp = string.Empty;
    private string? errorMessage;
    private bool isBusy;

    private StudentEditorViewModel(
        IStudentEditorService studentEditorService,
        IReadOnlyList<PlanCatalogItem> assignablePlans,
        bool requestCloseOnSave)
    {
        this.studentEditorService = studentEditorService;
        this.requestCloseOnSave = requestCloseOnSave;
        AssignablePlans = new ObservableCollection<PlanCatalogItem>(
            assignablePlans.Where(plan => plan.CanBeAssigned).OrderBy(plan => plan.Name));
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        RefreshSchedulePlacements();
    }

    public event EventHandler<bool?>? CloseRequested;

    public static StudentEditorViewModel CreateNew(
        IStudentEditorService studentEditorService,
        IReadOnlyList<PlanCatalogItem>? assignablePlans = null,
        bool requestCloseOnSave = true)
    {
        var viewModel = new StudentEditorViewModel(studentEditorService, assignablePlans ?? [], requestCloseOnSave);
        viewModel.requiresCatalogPlanAssignment = true;
        viewModel.SetSelectedAssignablePlan(viewModel.AssignablePlans.FirstOrDefault(), applyPlan: true);
        viewModel.AcceptChanges();
        return viewModel;
    }

    public static StudentEditorViewModel FromSnapshot(
        IStudentEditorService studentEditorService,
        StudentEditorSnapshot snapshot,
        IReadOnlyList<PlanCatalogItem>? assignablePlans = null,
        bool requestCloseOnSave = true)
    {
        var viewModel = new StudentEditorViewModel(studentEditorService, assignablePlans ?? [], requestCloseOnSave)
        {
            studentId = snapshot.StudentId,
            ShortName = snapshot.ShortName,
            FullName = snapshot.FullName,
            Phone = snapshot.Phone,
            Email = snapshot.Email,
            AgeBracket = snapshot.AgeBracket,
            BirthdateDate = snapshot.Birthdate?.ToDateTime(TimeOnly.MinValue),
            IsActive = snapshot.IsActive
        };
        var addressLines = SplitAddress(snapshot.Address);
        viewModel.Address = addressLines.Line1;
        viewModel.AddressLine2 = addressLines.Line2;

        var currentPlan = snapshot.PlanPeriods
            .OrderByDescending(plan => plan.StartsOn)
            .FirstOrDefault();
        if (currentPlan is not null)
        {
            viewModel.PlanName = currentPlan.PlanName;
            viewModel.planCatalogId = currentPlan.PlanCatalogId;
            viewModel.PlanPriceWithoutVat = currentPlan.PriceWithoutVat;
            viewModel.LessonsPerWeek = currentPlan.LessonsPerWeek;
            viewModel.PlanDurationMinutes = currentPlan.DurationMinutes;
            viewModel.PlanFlexibility = currentPlan.Flexibility;
            viewModel.PlanNumberOfStudents = currentPlan.NumberOfStudents;
            viewModel.PlanEffectiveDate = currentPlan.PlanEffectiveFrom.Date;
            viewModel.PlanPaymentTerms = currentPlan.PaymentTerms;
            viewModel.PlanStartsOnDate = currentPlan.StartsOn.ToDateTime(TimeOnly.MinValue);
            viewModel.PlanEndsOnDate = currentPlan.EndsOn?.ToDateTime(TimeOnly.MinValue);
            viewModel.SetSelectedAssignablePlan(
                viewModel.AssignablePlans.FirstOrDefault(plan => plan.Id == currentPlan.PlanCatalogId) ??
                viewModel.AssignablePlans.FirstOrDefault(plan =>
                    string.Equals(plan.Name, currentPlan.PlanName, StringComparison.OrdinalIgnoreCase)),
                applyPlan: false);
        }

        viewModel.ApplySchedule(snapshot.Schedule);
        viewModel.AcceptChanges();
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

    public ObservableCollection<PlanCatalogItem> AssignablePlans { get; }

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

    public string AddressLine2
    {
        get => addressLine2;
        set => SetProperty(ref addressLine2, value);
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

    public string PlanPriceWithoutVatText => PlanPriceWithoutVat.ToString("0.##", CultureInfo.InvariantCulture) + " kr.";

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

    public string PlanDurationMinutesText => $"{PlanDurationMinutes.ToString(CultureInfo.InvariantCulture)} minutes";

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

    public string PlanPaymentTerms
    {
        get => planPaymentTerms;
        set => SetProperty(ref planPaymentTerms, value);
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

    public PlanCatalogItem? SelectedAssignablePlan
    {
        get => selectedAssignablePlan;
        set => SetSelectedAssignablePlan(value, applyPlan: true);
    }

    public bool HasAssignablePlans => AssignablePlans.Count > 0;

    public bool HasNoAssignablePlans => !HasAssignablePlans;

    public bool HasSelectedPlan => SelectedAssignablePlan is not null;

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
            }
        }
    }

    public bool WasSaved { get; private set; }

    public bool HasChanges => CreateChangeStamp() != savedChangeStamp;

    public AsyncRelayCommand SaveCommand { get; }

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
            AcceptChanges();
            ErrorMessage = null;
            if (requestCloseOnSave)
            {
                CloseRequested?.Invoke(this, true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private SaveStudentRequest? CreateRequest()
    {
        if (requiresCatalogPlanAssignment && SelectedAssignablePlan?.CanBeAssigned != true)
        {
            ErrorMessage = "An active plan is required before this student can be saved.";
            return null;
        }

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
            studentId,
            ShortName,
            FullName,
            CombineAddress(Address, AddressLine2),
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
                    PlanEndsOnDate is null ? null : DateOnly.FromDateTime(PlanEndsOnDate.Value),
                    PlanPaymentTerms,
                    planCatalogId)
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

    private void SetSelectedAssignablePlan(PlanCatalogItem? plan, bool applyPlan)
    {
        if (!SetProperty(ref selectedAssignablePlan, plan, nameof(SelectedAssignablePlan)))
        {
            return;
        }

        OnPropertyChanged(nameof(HasSelectedPlan));

        if (applyPlan && plan is not null)
        {
            ApplyCatalogPlan(plan);
        }
    }

    private void ApplyCatalogPlan(PlanCatalogItem plan)
    {
        planCatalogId = plan.Id;
        PlanName = plan.Name;
        PlanPriceWithoutVat = plan.PriceWithoutVat ?? 0m;
        LessonsPerWeek = plan.LessonsPerWeek;
        PlanDurationMinutes = plan.DurationMinutes;
        PlanFlexibility = plan.Flexibility;
        PlanNumberOfStudents = plan.NumberOfStudents;
        PlanEffectiveDate = plan.EffectiveFrom.Date;
        PlanPaymentTerms = plan.PaymentTerms;
    }

    private static (string Line1, string Line2) SplitAddress(string address)
    {
        var normalized = address.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var separatorIndex = normalized.IndexOf('\n', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (address, string.Empty);
        }

        var line1 = normalized[..separatorIndex];
        var line2 = normalized[(separatorIndex + 1)..].Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        return (line1, line2);
    }

    private static string CombineAddress(string line1, string line2)
    {
        var trimmedLine1 = line1.Trim();
        var trimmedLine2 = line2.Trim();
        return string.IsNullOrWhiteSpace(trimmedLine2)
            ? trimmedLine1
            : trimmedLine1 + Environment.NewLine + trimmedLine2;
    }

    private void AcceptChanges()
    {
        savedChangeStamp = CreateChangeStamp();
    }

    private string CreateChangeStamp()
    {
        var schedule = string.Join(
            "|",
            SchedulePlacements.Select(placement =>
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{placement.SelectedWeekday.Weekday?.ToString() ?? string.Empty}:{placement.TimeText}")));

        return string.Join(
            "\u001F",
            ShortName,
            FullName,
            Address,
            AddressLine2,
            Phone,
            Email,
            AgeBracket.ToString(),
            BirthdateDate?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            IsActive ? bool.TrueString : bool.FalseString,
            SelectedAssignablePlan?.Id.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            PlanName,
            PlanPriceWithoutVat.ToString(CultureInfo.InvariantCulture),
            LessonsPerWeek.ToString(),
            PlanDurationMinutes.ToString(CultureInfo.InvariantCulture),
            PlanFlexibility.ToString(),
            PlanNumberOfStudents.ToString(CultureInfo.InvariantCulture),
            PlanEffectiveDate.ToString("O", CultureInfo.InvariantCulture),
            PlanPaymentTerms,
            PlanStartsOnDate.ToString("O", CultureInfo.InvariantCulture),
            PlanEndsOnDate?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            schedule);
    }
}
