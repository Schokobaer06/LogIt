using LogIt.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Data;

public class LogItDbContext : DbContext
{
    public LogItDbContext(DbContextOptions<LogItDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<LogEntry>()
            .HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.LogEntry)
            .WithMany(le => le.Sessions)
            .HasForeignKey(s => s.LogEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}
