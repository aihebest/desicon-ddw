namespace Ddw.Api.Domain;

public enum Priority { Low = 0, Normal = 1, High = 2, Critical = 3 }

public enum AnnouncementStatus { Draft = 0, Published = 1, Withdrawn = 2 }

// How an announcement is targeted. "All" reaches everyone; the others match a value.
public enum ScopeType { All = 0, Department = 1, Location = 2, Project = 3, Role = 4 }

// What the recipient did with a notification.
public enum AckAction { Delivered = 0, Read = 1, Acknowledged = 2 }

public class Announcement
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Category { get; set; } = "General"; // News/HSE/ICT/HR/Project...
    public Priority Priority { get; set; } = Priority.Normal;
    public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Published;
    public bool RequiresAck { get; set; }
    public bool RepeatUntilRead { get; set; } = true;
    public DateTime PublishAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AnnouncementTarget> Targets { get; set; } = new();
    public List<Acknowledgment> Acknowledgments { get; set; } = new();
}

public class AnnouncementTarget
{
    public long Id { get; set; }
    public long AnnouncementId { get; set; }
    public ScopeType ScopeType { get; set; }
    public string? ScopeValue { get; set; } // e.g. "Engineering", "Bonny", "NLNG Train 7"
    public Announcement? Announcement { get; set; }
}

public class Acknowledgment
{
    public long Id { get; set; }
    public long AnnouncementId { get; set; }
    public string UserUpn { get; set; } = "";
    public AckAction Action { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    public string? DeviceName { get; set; }
    public Announcement? Announcement { get; set; }
}
