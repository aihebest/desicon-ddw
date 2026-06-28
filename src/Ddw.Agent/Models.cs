namespace Ddw.Agent;

// Saved locally on first run; sent to the server so it can target this user.
public class UserConfig
{
    public string Upn { get; set; } = "";
    public string Department { get; set; } = "";
    public string Location { get; set; } = "";   // Port Harcourt / Lagos / Bonny / ...
    public string Project { get; set; } = "";
    public string DeviceName { get; set; } = Environment.MachineName;
}

// Mirrors the server's UserContext (POST /api/v1/notifications/poll).
public record UserContext(string Upn, string? Department, string? Location, string? Project, string? Role, string? DeviceName);

// Mirrors the server's NotificationDto.
public record NotificationDto(
    long Id, string Title, string Body, string Category, int Priority,
    bool RequiresAck, bool RepeatUntilRead, DateTime PublishAt);

public record AckBody(string Upn, string? DeviceName);

public static class Priorities
{
    public const int Low = 0, Normal = 1, High = 2, Critical = 3;
    public static string Name(int p) => p switch { 3 => "CRITICAL", 2 => "HIGH", 1 => "NORMAL", _ => "INFO" };
}
