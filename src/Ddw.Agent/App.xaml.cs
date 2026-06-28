using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Ddw.Agent;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon _tray = null!;
    private DispatcherTimer _timer = null!;
    private UserConfig _config = new();
    private readonly Dictionary<long, PopupWindow> _open = new();

    private static System.Threading.Mutex? _single;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance — stop a stale build from running alongside this one.
        _single = new System.Threading.Mutex(true, "DesiconAgent.SingleInstance", out var isNew);
        if (!isNew) { AgentLog.Write("another instance already running — exiting"); Shutdown(); return; }
        AgentLog.Write("agent starting (build with token + logging)");

        // Sign in with the employee's Microsoft 365 account (interactive once, silent after).
        string upn;
        try
        {
            (_, upn) = await AuthService.GetTokenAsync(allowInteractive: true);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Sign-in is required to use Desicon Alerts.\n\n" + ex.Message,
                "Desicon Alerts", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        // Identity comes from Entra; collect site details on first run.
        var cfg = ConfigStore.Load() ?? new UserConfig();
        cfg.Upn = upn;
        if (string.IsNullOrWhiteSpace(cfg.Location))
        {
            var win = new SetupWindow(cfg);
            if (win.ShowDialog() != true) { Shutdown(); return; }
            cfg = win.Result;
        }
        cfg.Upn = upn;
        ConfigStore.Save(cfg);
        _config = cfg;

        StartupRegistration.EnsureRegistered();
        BuildTray();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _timer.Tick += async (_, _) => await PollAndShow();
        _timer.Start();

        await PollAndShow(); // login check — catch anything queued
    }

    private void BuildTray()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Check for announcements", null, async (_, _) => await PollAndShow());
        menu.Items.Add("My details…", null, (_, _) => ShowSetup());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _tray = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Desicon Alerts",
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += async (_, _) => await PollAndShow();
    }

    private async Task PollAndShow()
    {
        try
        {
            var (token, _) = await AuthService.GetTokenAsync(allowInteractive: false);
            var notes = await ApiClient.PollAsync(_config, token!);
            AgentLog.Write($"poll: API returned {notes.Count} notification(s)");
            foreach (var n in notes)
            {
                if (_open.ContainsKey(n.Id)) continue; // already on screen
                var popup = new PopupWindow(n, _config);
                popup.Closed += (_, _) => _open.Remove(n.Id);
                _open[n.Id] = popup;
                popup.Show();
            }
            _tray.Text = notes.Count > 0 ? $"Desicon Alerts — {notes.Count} unread" : "Desicon Alerts";
        }
        catch (Exception ex)
        {
            AgentLog.Write("poll FAILED: " + ex.Message);
        }
    }

    private bool ShowSetup()
    {
        var win = new SetupWindow(ConfigStore.Load() ?? _config);
        if (win.ShowDialog() == true)
        {
            _config = win.Result;
            ConfigStore.Save(_config);
            return true;
        }
        return ConfigStore.Load() is not null;
    }

    private void ExitApp()
    {
        _timer?.Stop();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        Shutdown();
    }
}
