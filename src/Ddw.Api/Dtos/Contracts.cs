using Ddw.Api.Domain;

namespace Ddw.Api.Dtos;

// ----- Admin: create an announcement -----
public record TargetDto(ScopeType ScopeType, string? ScopeValue);

public record CreateAnnouncementDto(
    string Title,
    string Body,
    string Category,
    Priority Priority,
    bool RequiresAck,
    bool RepeatUntilRead,
    DateTime? PublishAt,
    DateTime? ExpiresAt,
    List<TargetDto> Targets);

// ----- Agent: identify the signed-in user when polling -----
public record UserContext(
    string Upn,
    string? Department,
    string? Location,
    string? Project,
    string? Role,
    string? DeviceName);

// ----- Agent: what the popup shows -----
public record NotificationDto(
    long Id,
    string Title,
    string Body,
    string Category,
    Priority Priority,
    bool RequiresAck,
    bool RepeatUntilRead,
    DateTime PublishAt);

// ----- Analytics -----
public record AnnouncementStats(
    long AnnouncementId,
    string Title,
    Priority Priority,
    int Delivered,
    int Read,
    int Acknowledged);

public record AnalyticsSummary(
    int TotalAnnouncements,
    int Published,
    int TotalDeliveries,
    int TotalReads,
    int TotalAcknowledged,
    int PendingUnread,
    List<AnnouncementStats> PerAnnouncement);
