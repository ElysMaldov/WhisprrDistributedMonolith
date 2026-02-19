using Microsoft.EntityFrameworkCore;
using Whisprr.Entities.Models;

namespace Whisprr.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) // Pass in options when we initialize the connection in Program.cs
{
  public DbSet<SocialInfo> SocialInfos { get; set; }
  public DbSet<SourcePlatform> SourcePlatforms { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure models using fluent API
    modelBuilder.Entity<SourcePlatform>()
        .HasIndex(s => s.Name)
        .IsUnique();

    // Optional: our relationships should be discoverable by convention
    // modelBuilder.Entity<SocialInfo>()
    //     .HasOne(s => s.SourcePlatform)
    //     .WithMany(p => p.SocialInfos)
    //     .HasForeignKey(s => s.SourcePlatformId);
  }
}