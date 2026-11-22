using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using ElenasBusinessLedger.App.Services;

namespace ElenasBusinessLedger.Tests.App;

// Runs WPF tests on an STA thread with a dispatcher and disposable layout files.
internal static class WpfTestThread
{
    public static void Run(Action action) => Run(() =>
    {
        action();
        return Task.CompletedTask;
    });

    public static void Run(Func<Task> action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                RunWithTemporaryPreferences(action);
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    private static void RunWithTemporaryPreferences(Func<Task> action)
    {
        var preferences = Directory.CreateTempSubdirectory("LedgerWpfTests-");
        var windows = Path.Combine(preferences.FullName, "window-placement.json");
        var grids = Path.Combine(preferences.FullName, "grid-column-widths.json");
        var dispatcher = Dispatcher.CurrentDispatcher;
        var previousContext = SynchronizationContext.Current;
        var previousWindowPath = WindowPlacementStore.StorePathOverride;
        var previousGridPath = GridWidthStore.StorePathOverride;
        WindowPlacementStore.StorePathOverride = windows;
        GridWidthStore.StorePathOverride = grids;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        try
        {
            var operation = action();
            if (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                _ = operation.ContinueWith(
                    _ => dispatcher.InvokeAsync(() => frame.Continue = false),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                Dispatcher.PushFrame(frame);
            }

            operation.GetAwaiter().GetResult();
        }
        finally
        {
            dispatcher.InvokeShutdown();
            SynchronizationContext.SetSynchronizationContext(previousContext);
            GridWidthStore.StorePathOverride = previousGridPath;
            WindowPlacementStore.StorePathOverride = previousWindowPath;
            File.Delete(grids);
            File.Delete(windows);
            preferences.Delete();
        }
    }
}
