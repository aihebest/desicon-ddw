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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // First run: collect the user's details.
        var existing = ConfigStore.Load();
        if (existing is null || string.IsNullOrWhiteSpace(existing.Upn))
        {
            if (!ShowSetup()) { Shutdown(); return; }
        }
        else { _config = existing; }

        StartupRegistration.EnsureRegistered();
        BuildTray();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _timer.Tick += async (_, _) => await PollAndShow();
        _timer.Start();

        // Login check — catch anything queued while signed out.
        Dispatcher.BeginInvoke(async () => await PollAndShow(), DispatcherPriority.ApplicationIdle);
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
            var notes = await ApiClient.PollAsync(_config);
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
        catch { /* offline / server unreachable — try again next tick */ }
    }

    private bool ShowSetup()
    {
        var win = new SetupWindow(ConfigStore.Load() ?? _config);
        if (win.ShowDialog() == true) { _config = win.Result; ConfigStore.Save(_config); return true; }
        return ConfigStore.Load() is not null; // keep running if already configured
    }

    private void ExitApp()
    {
        _timer?.Stop();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        Shutdown();
    }
}
