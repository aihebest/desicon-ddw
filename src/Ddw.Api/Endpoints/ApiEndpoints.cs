using Ddw.Api.Data;
using Ddw.Api.Domain;
using Ddw.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Ddw.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapDdwApi(this WebApplication app)
    {
        // Liveness/readiness for the App Service health check.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Ddw.Api" }))
           .WithTags("System");

        var admin = app.MapGroup("/api/v1").AddEndpointFilter(new AdminKeyFilter());
        var agent = app.MapGroup("/api/v1");

        // ---------- ADMIN: create / list ----------
        admin.MapPost("/announcements", async (CreateAnnouncementDto dto, DdwDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return Results.BadRequest("Title is required.");
            if (dto.Targets is null || dto.Targets.Count == 0)
                return Results.BadRequest("At least one target is required (use ScopeType.All for everyone).");

            var a = new Announcement
            {
                Title = dto.Title,
                Body = dto.Body ?? "",
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category,
                Priority = dto.Priority,
                RequiresAck = dto.RequiresAck,
                RepeatUntilRead = dto.RepeatUntilRead,
                PublishAt = dto.PublishAt ?? DateTime.UtcNow,
                ExpiresAt = dto.ExpiresAt,
                Status = AnnouncementStatus.Published,
                CreatedBy = "admin-portal",
                Targets = dto.Targets.Select(t => new AnnouncementTarget
                {
                    ScopeType = t.ScopeType,
                    ScopeValue = t.ScopeType == ScopeType.All ? null : t.ScopeValue
                }).ToList()
            };
            db.Announcements.Add(a);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/announcements/{a.Id}", new { a.Id, a.Title, recipientsTargeted = a.Targets.Count });
        }).WithTags("Admin");

        admin.MapGet("/announcements", async (DdwDbContext db) =>
            await db.Announcements.Include(a => a.Targets)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new { a.Id, a.Title, a.Category, a.Priority, a.Status, a.PublishAt, a.ExpiresAt, a.RequiresAck, Targets = a.Targets.Select(t => new { t.ScopeType, t.ScopeValue }) })
                .ToListAsync())
            .WithTags("Admin");

        // ---------- AGENT: poll for my notifications (the targeting engine) ----------
        agent.MapPost("/notifications/poll", async (UserContext me, DdwDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(me.Upn)) return Results.BadRequest("Upn is required.");
            var now = DateTime.UtcNow;

            var matches = await db.Announcements
                .Where(a => a.Status == AnnouncementStatus.Published
                    && a.PublishAt <= now
                    && (a.ExpiresAt == null || a.ExpiresAt > now))
                .Where(a => a.Targets.Any(t =>
                    t.ScopeType == ScopeType.All
                    || (t.ScopeType == ScopeType.Department && me.Department != null && t.ScopeValue == me.Department)
                    || (t.ScopeType == ScopeType.Location && me.Location != null && t.ScopeValue == me.Location)
                    || (t.ScopeType == ScopeType.Project && me.Project != null && t.ScopeValue == me.Project)
                    || (t.ScopeType == ScopeType.Role && me.Role != null && t.ScopeValue == me.Role)))
                // only what this user has NOT yet read/acknowledged
                .Where(a => !a.Acknowledgments.Any(ack => ack.UserUpn == me.Upn && (int)ack.Action >= (int)AckAction.Read))
                .OrderByDescending(a => a.Priority).ThenByDescending(a => a.PublishAt)
                .ToListAsync();

            // Record a delivery the first time each one is pushed to this user.
            var ids = matches.Select(a => a.Id).ToList();
            var delivered = await db.Acknowledgments
                .Where(ack => ack.UserUpn == me.Upn && ack.Action == AckAction.Delivered && ids.Contains(ack.AnnouncementId))
                .Select(ack => ack.AnnouncementId).ToListAsync();
            foreach (var a in matches.Where(a => !delivered.Contains(a.Id)))
                db.Acknowledgments.Add(new Acknowledgment { AnnouncementId = a.Id, UserUpn = me.Upn, Action = AckAction.Delivered, DeviceName = me.DeviceName });
            if (matches.Count > delivered.Count) await db.SaveChangesAsync();

            var result = matches.Select(a => new NotificationDto(
                a.Id, a.Title, a.Body, a.Category, a.Priority, a.RequiresAck, a.RepeatUntilRead, a.PublishAt));
            return Results.Ok(result);
        }).WithTags("Agent");

        agent.MapPost("/notifications/{id:long}/read", (long id, AckBody body, DdwDbContext db)
            => RecordAck(id, body, AckAction.Read, db)).WithTags("Agent");

        agent.MapPost("/notifications/{id:long}/ack", (long id, AckBody body, DdwDbContext db)
            => RecordAck(id, body, AckAction.Acknowledged, db)).WithTags("Agent");

        // ---------- ADMIN: analytics ----------
        admin.MapGet("/analytics", async (DdwDbContext db) =>
        {
            var anns = await db.Announcements.Include(a => a.Acknowledgments).ToListAsync();
            var per = new List<AnnouncementStats>();
            int totalDeliveries = 0, totalReads = 0, totalAck = 0, pending = 0;
            foreach (var a in anns)
            {
                var delivered = a.Acknowledgments.Select(x => x.UserUpn).Distinct().Count();
                var read = a.Acknowledgments.Where(x => (int)x.Action >= (int)AckAction.Read).Select(x => x.UserUpn).Distinct().Count();
                var ack = a.Acknowledgments.Where(x => x.Action == AckAction.Acknowledged).Select(x => x.UserUpn).Distinct().Count();
                totalDeliveries += delivered; totalReads += read; totalAck += ack; pending += Math.Max(0, delivered - read);
                per.Add(new AnnouncementStats(a.Id, a.Title, a.Priority, delivered, read, ack));
            }
            return Results.Ok(new AnalyticsSummary(
                anns.Count,
                anns.Count(a => a.Status == AnnouncementStatus.Published),
                totalDeliveries, totalReads, totalAck, pending,
                per.OrderByDescending(p => p.Delivered).ToList()));
        }).WithTags("Admin");
    }

    private static async Task<IResult> RecordAck(long id, AckBody body, AckAction action, DdwDbContext db)
    {
        if (string.IsNullOrWhiteSpace(body.Upn)) return Results.BadRequest("Upn is required.");
        var exists = await db.Announcements.AnyAsync(a => a.Id == id);
        if (!exists) return Results.NotFound();

        var already = await db.Acknowledgments
            .AnyAsync(x => x.AnnouncementId == id && x.UserUpn == body.Upn && x.Action == action);
        if (!already)
        {
            db.Acknowledgments.Add(new Acknowledgment
            {
                AnnouncementId = id,
                UserUpn = body.Upn,
                Action = action,
                DeviceName = body.DeviceName
            });
            await db.SaveChangesAsync();
        }
        return Results.Ok(new { announcementId = id, user = body.Upn, status = action.ToString() });
    }
}

public record AckBody(string Upn, string? DeviceName);

// Minimal admin guard: an X-Api-Key header must match the configured key.
// (Replace with Entra ID JWT auth in the next iteration.)
public class AdminKeyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var cfg = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = cfg["AdminApiKey"];
        if (string.IsNullOrEmpty(expected))
            return Results.Problem("AdminApiKey is not configured on the server.", statusCode: 500);
        var provided = ctx.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (provided != expected)
            return Results.Json(new { error = "Invalid or missing X-Api-Key." }, statusCode: 401);
        return await next(ctx);
    }
}
