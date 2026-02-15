using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database.Models;
using Sunrise.Shared.Database.Models.Beatmap;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Events;
using Sunrise.Shared.Database.Models.Users;

namespace Sunrise.Shared.Database;

public class SunriseDbContext : DbContext
{
    public SunriseDbContext()
    {
    }

    public SunriseDbContext(DbContextOptions<SunriseDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRelationship> UserRelationships { get; set; }
    public DbSet<UserFavouriteBeatmap> UserFavouriteBeatmaps { get; set; }
    public DbSet<UserMedals> UserMedals { get; set; }
    public DbSet<UserMetadata> UserMetadata { get; set; }
    public DbSet<UserStats> UserStats { get; set; }
    public DbSet<UserGrades> UserGrades { get; set; }
    public DbSet<UserStatsSnapshot> UserStatsSnapshot { get; set; }
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<UserInventoryItem> UserInventoryItem { get; set; }


    public DbSet<Medal> Medals { get; set; }
    public DbSet<MedalFile> MedalFiles { get; set; }

    public DbSet<EventBeatmap> EventBeatmaps { get; set; }
    public DbSet<EventUser> EventUsers { get; set; }
    public DbSet<Restriction> Restrictions { get; set; }

    public DbSet<Score> Scores { get; set; }

    public DbSet<BeatmapHype> BeatmapHypes { get; set; }
    public DbSet<CustomBeatmapStatus> CustomBeatmapStatuses { get; set; }

    public DbSet<Clan> Clans { get; set; }
    public DbSet<ClanMember> ClanMembers { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .UseCollation("utf8mb4_unicode_ci");

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .UseCollation("utf8mb4_unicode_ci");

        modelBuilder.Entity<UserRelationship>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserInitiatedRelationships)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRelationship>()
            .HasOne(ur => ur.Target)
            .WithMany(u => u.UserReceivedRelationships)
            .HasForeignKey(ur => ur.TargetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Clan)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClanMember>()
            .HasOne(cm => cm.Clan)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.ClanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClanMember>()
            .HasOne(cm => cm.User)
            .WithMany()
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySQL(Configuration.DatabaseConnectionString);
        }
    }
}