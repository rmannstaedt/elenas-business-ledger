using MySmallBusinessLedger.Core.Common;

namespace MySmallBusinessLedger.Core.Students;

public sealed record Student(
    string ShortName,
    string FullName,
    string Address,
    string Phone,
    string Email,
    AgeBracket AgeBracket,
    DateOnly? Birthdate,
    bool IsActive)
{
    public static ValidationResult<Student> Create(
        string shortName,
        string fullName,
        string address,
        string phone,
        string email,
        AgeBracket ageBracket,
        DateOnly? birthdate,
        bool isActive)
    {
        if (ageBracket == AgeBracket.Young && birthdate is null)
        {
            return ValidationResult<Student>.Failure(new ValidationError(
                "Student.BirthdateRequiredForYoung",
                "Birthdate is mandatory when the student age bracket is young."));
        }

        return ValidationResult<Student>.Success(new Student(
            shortName,
            fullName,
            address,
            phone,
            email,
            ageBracket,
            birthdate,
            isActive));
    }
}
