using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;
using Whisprr.Entities.Enums;
using Whisprr.Entities.Interfaces;
using Whisprr.Entities.Models;

namespace Whisprr.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) // Pass in options when we initialize the connection in Program.cs
{
  public DbSet<SocialInfo> SocialInfos { get; set; }
  public DbSet<SourcePlatform> SourcePlatforms { get; set; }
  public DbSet<SocialTopic> SocialTopics { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Setup enums
    modelBuilder.HasPostgresEnum<Sentiment>();

    // Configure models using fluent API
    modelBuilder.Entity<SourcePlatform>()
        .HasIndex(s => s.Name)
        .IsUnique();

    // Optional: our relationships should be discoverable by convention
    // modelBuilder.Entity<SocialInfo>()
    //     .HasOne(s => s.SourcePlatform)
    //     .WithMany(p => p.SocialInfos)
    //     .HasForeignKey(s => s.SourcePlatformId);

    modelBuilder.Entity(ConvertSocialTopicLanguage());

    modelBuilder.Entity<SocialTopicListeningTask>()
        .HasMany(t => t.GeneratedSocialInfos)
        .WithOne(s => s.GeneratedFromTask)
        .HasForeignKey(s => s.GeneratedFromTaskId);
  }

  private static Action<EntityTypeBuilder<SocialTopic>> ConvertSocialTopicLanguage()
  {
    return entity =>
    {
      entity.Property(e => e.Language)
         .HasConversion(
             v => v.Name,
             v => new CultureInfo(v)
         );
    };
  }

  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    UpdateTrackableModels();

    return base.SaveChangesAsync(cancellationToken);
  }

  private void UpdateTrackableModels()
  {
    var now = DateTimeOffset.UtcNow;

    var entries = ChangeTracker.Entries<ITrackableModel>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

    foreach (var entry in entries)
    {

      entry.Entity.UpdatedAt = now;

      if (entry.State == EntityState.Added)
      {
        entry.Entity.CreatedAt = now;
      }
    }
  }
}