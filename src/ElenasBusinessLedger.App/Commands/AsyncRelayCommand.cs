using System.Windows.Input;

namespace ElenasBusinessLedger.App.Commands;

// Adapts asynchronous operations to WPF commands while preventing concurrent executions of the same command.
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> execute;
    private readonly Predicate<object?>? canExecute;
    private bool isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        !isExecuting && (canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    public Task ExecuteAsync() => ExecuteAsync(null);

    // Runs a command once at a time and updates WPF command availability around the asynchronous work.
    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            isExecuting = true;
            RaiseCanExecuteChanged();
            await execute(parameter);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
