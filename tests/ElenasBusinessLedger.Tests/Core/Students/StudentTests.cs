using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.Tests.Core.Students;

public sealed class StudentTests
{
    [Fact]
    public void CreateStandardStudentAllowsMissingBirthdate()
    {
        var result = Student.Create(
            shortName: "Anna",
            fullName: "Anna Andersen",
            address: "Testvej 1",
            phone: "12345678",
            email: "anna@example.test",
            ageBracket: AgeBracket.Standard,
            birthdate: null,
            isActive: true);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("Anna", result.Value.ShortName);
    }

    [Fact]
    public void CreateYoungStudentRequiresBirthdate()
    {
        var result = Student.Create(
            shortName: "Ida",
            fullName: "Ida Iversen",
            address: "Testvej 2",
            phone: "87654321",
            email: "ida@example.test",
            ageBracket: AgeBracket.Young,
            birthdate: null,
            isActive: true);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Student.BirthdateRequiredForYoung");
    }
}
