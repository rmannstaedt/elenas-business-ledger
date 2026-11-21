using ElenasBusinessLedger.App.Services;

namespace ElenasBusinessLedger.App.ViewModels;

public sealed class StudentOverviewStudentDisplayModel : ViewModelBase
{
    public StudentOverviewStudentDisplayModel(StudentOverviewSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public StudentOverviewSnapshot Snapshot { get; private set; }

    public string ShortName => Snapshot.Student.ShortName;

    public string FullName => Snapshot.Student.FullName;

    public string Phone => Snapshot.Student.Phone;

    public string Email => Snapshot.Student.Email;

    public bool IsActive => Snapshot.Student.IsActive;

    public string StateText => IsActive ? "Active" : "Inactive";

    public void Update(StudentOverviewSnapshot snapshot)
    {
        Snapshot = snapshot;
        OnPropertyChanged(nameof(ShortName));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StateText));
    }
}
