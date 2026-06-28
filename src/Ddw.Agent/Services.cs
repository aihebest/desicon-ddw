using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Win32;

namespace Ddw.Agent;

// Local config (the user's email / location / department / project).
public static class ConfigStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesiconAgent");
    private static readonly string FilePath = Path.Combine(Dir, "config.json");

    public static UserConfig? Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<UserConfig>(File.ReadAllText(FilePath)) : null; }
        catch { return null; }
    }

    public static void Save(UserConfig cfg)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }
}

// Talks to the live DDW API. Poll endpoints are open; the agent sends the user's context.
public static class ApiClient
{
    public const string BaseUrl = "https://app-ddw-dev-x6zi99.azurewebsites.net";
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<List<NotificationDto>> PollAsync(UserConfig c)
    {
        var ctx = new UserContext(c.Upn, NullIfEmpty(c.Department), NullIfEmpty(c.Location),
                                  NullIfEmpty(c.Project), null, c.DeviceName);
        var resp = await Http.PostAsJsonAsync("/api/v1/notifications/poll", ctx);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? new();
    }

    public static Task ReadAsync(long id, UserConfig c) => Post($"/api/v1/notifications/{id}/read", c);
    public static Task AckAsync(long id, UserConfig c) => Post($"/api/v1/notifications/{id}/ack", c);

    private static async Task Post(string path, UserConfig c)
    {
        try { await Http.PostAsJsonAsync(path, new AckBody(c.Upn, c.DeviceName)); } catch { /* best effort */ }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

// Registers the agent to launch silently at Windows login (per-user, no admin needed).
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesiconAgent";

    public static void EnsureRegistered()
    {
        try
        {
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) return;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.SetValue(ValueName, $"\"{exe}\"");
        }
        catch { /* non-fatal */ }
    }
}
