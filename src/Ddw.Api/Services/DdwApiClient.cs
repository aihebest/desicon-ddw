using System.Net.Http.Json;
using Ddw.Api.Dtos;

namespace Ddw.Api.Services;

// Server-side client the Blazor portal uses to call the DDW API (in-process,
// authenticated with the admin key from Key Vault — never exposed to the browser).
public class DdwApiClient(HttpClient http)
{
    public async Task<AnalyticsSummary?> GetAnalyticsAsync()
        => await http.GetFromJsonAsync<AnalyticsSummary>("/api/v1/analytics");

    public async Task<List<AnnouncementListItem>?> GetAnnouncementsAsync()
        => await http.GetFromJsonAsync<List<AnnouncementListItem>>("/api/v1/announcements");

    public async Task<(bool ok, string message)> CreateAsync(CreateAnnouncementDto dto)
    {
        var resp = await http.PostAsJsonAsync("/api/v1/announcements", dto);
        var body = await resp.Content.ReadAsStringAsync();
        return (resp.IsSuccessStatusCode, body);
    }
}

// Shape returned by GET /api/v1/announcements.
public record AnnouncementListItem(
    long Id, string Title, string Category, int Priority, int Status,
    DateTime PublishAt, DateTime? ExpiresAt, bool RequiresAck,
    List<AnnouncementTargetItem> Targets);

public record AnnouncementTargetItem(int ScopeType, string? ScopeValue);
