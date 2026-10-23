using System.Windows;
using System.Windows.Threading;
using AllyConsole.App.Hosting;
using AllyConsole.App.Windows;

namespace AllyConsole.App;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _show;
    private RegisteredWaitHandle? _showWait;
    private CompanionController? _controller;
    private MainWindow? _window;
    private TrayHost? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += OnDispatcherException;

        var developmentMode = e.Args.Any(arg => string.Equals(arg, "--dev", StringComparison.Ordinal));
        _mutex = new Mutex(true, @"Local\AllyConsole.SingleInstance", out var created);
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\AllyConsole.Show");
        if (!created)
        {
            _show.Set();
            Shutdown();
            return;
        }

        _showWait = ThreadPool.RegisterWaitForSingleObject(_show, OnShowRequested, null, Timeout.Infinite, executeOnlyOnce: false);
        var context = SynchronizationContext.Current ?? new DispatcherSynchronizationContext(Dispatcher);
        _controller = new CompanionController(developmentMode, context);
        _controller.ExitRequested += ExitCompanion;
        _window = new MainWindow();
        MainWindow = _window;
        _window.Bind(_controller);
        var hwnd = _window.EnsureHotkeyWindow();
        _controller.AttachHotkey(hwnd);
        _controller.Start();
        _tray = new TrayHost(() => _controller.OpenPanel(), () => _controller.Exit());
    }

    private void OnShowRequested(object? state, bool timedOut)
    {
        Dispatcher.BeginInvoke(() => _controller?.OpenPanel());
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _controller?.ReportFault(e.Exception);
        e.Handled = true;
    }

    private void ExitCompanion()
    {
        _window?.PrepareForExit();
        _tray?.Dispose();
        _showWait?.Unregister(null);
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        Shutdown();
    }
}
