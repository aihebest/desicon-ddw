using Ddw.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ddw.Api.Data;

public class DdwDbContext(DbContextOptions<DdwDbContext> options) : DbContext(options)
{
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementTarget> AnnouncementTargets => Set<AnnouncementTarget>();
    public DbSet<Acknowledgment> Acknowledgments => Set<Acknowledgment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Announcement>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Category).HasMaxLength(60);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Status, x.PublishAt });
        });

        b.Entity<AnnouncementTarget>(e =>
        {
            e.Property(x => x.ScopeValue).HasMaxLength(150);
            e.HasIndex(x => new { x.ScopeType, x.ScopeValue });
            e.HasOne(x => x.Announcement).WithMany(a => a.Targets)
                .HasForeignKey(x => x.AnnouncementId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Acknowledgment>(e =>
        {
            e.Property(x => x.UserUpn).HasMaxLength(256).IsRequired();
            e.Property(x => x.DeviceName).HasMaxLength(150);
            e.HasIndex(x => new { x.AnnouncementId, x.UserUpn, x.Action });
            e.HasOne(x => x.Announcement).WithMany(a => a.Acknowledgments)
                .HasForeignKey(x => x.AnnouncementId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
