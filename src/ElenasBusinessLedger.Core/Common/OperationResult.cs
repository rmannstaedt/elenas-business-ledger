namespace ElenasBusinessLedger.Core.Common;

// Carries a workflow result, failures, or a confirmation request so callers can keep useful state on screen.
public sealed class OperationResult<T>
{
    private OperationResult(T? value, bool succeeded, bool requiresConfirmation, IReadOnlyList<ValidationError> errors)
    {
        Value = value;
        Succeeded = succeeded;
        RequiresConfirmation = requiresConfirmation;
        Errors = errors;
    }

    public T? Value { get; }

    public bool Succeeded { get; }

    public bool RequiresConfirmation { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static OperationResult<T> Success(T value) => new(value, succeeded: true, requiresConfirmation: false, []);

    public static OperationResult<T> Failure(params ValidationError[] errors) =>
        new(default, succeeded: false, requiresConfirmation: false, errors);

    // Carries the current value alongside errors so the caller can still show known or partial state.
    public static OperationResult<T> Failure(T currentValue, params ValidationError[] errors) =>
        new(currentValue, succeeded: false, requiresConfirmation: false, errors);

    // Carries the current value and asks the caller for confirmation before the operation proceeds.
    public static OperationResult<T> ConfirmationRequired(T currentValue, ValidationError error) =>
        new(currentValue, succeeded: false, requiresConfirmation: true, [error]);
}
