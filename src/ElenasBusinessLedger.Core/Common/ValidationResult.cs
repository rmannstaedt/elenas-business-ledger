namespace ElenasBusinessLedger.Core.Common;

// Returns a validated value or errors so editors can correct input before data is saved.
public sealed class ValidationResult<T>
{
    private ValidationResult(T? value, IReadOnlyList<ValidationError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public bool Succeeded => Errors.Count == 0;

    public T? Value { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static ValidationResult<T> Success(T value) => new(value, []);

    public static ValidationResult<T> Failure(params ValidationError[] errors) => new(default, errors);
}
