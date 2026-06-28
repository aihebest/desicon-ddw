using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public const string BaseUrl = "https://alerts.desiconapp.com";
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<List<NotificationDto>> PollAsync(UserConfig c, string token)
    {
        var ctx = new UserContext(c.Upn, NullIfEmpty(c.Department), NullIfEmpty(c.Location),
                                  NullIfEmpty(c.Project), null, c.DeviceName);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/notifications/poll")
        { Content = JsonContent.Create(ctx) };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await Http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? new();
    }

    public static Task ReadAsync(long id, UserConfig c, string token) => Post($"/api/v1/notifications/{id}/read", c, token);
    public static Task AckAsync(long id, UserConfig c, string token) => Post($"/api/v1/notifications/{id}/ack", c, token);

    private static async Task Post(string path, UserConfig c, string token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            { Content = JsonContent.Create(new AckBody(c.Upn, c.DeviceName)) };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await Http.SendAsync(req);
        }
        catch { /* best effort */ }
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
