using LogIt.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Data;

/// <summary>
/// Represents the Entity Framework Core database context for the LogIt application.
/// Manages access to Users, LogEntries, and Sessions, and configures their relationships.
/// </summary>
public class LogItDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogItDbContext"/> class using the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public LogItDbContext(DbContextOptions<LogItDbContext> options)
        : base(options) { }

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> representing the Users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> representing the LogEntries table.
    /// </summary>
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> representing the Sessions table.
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// Configures the entity relationships and property conversions for the model.
    /// 
    /// Builder method summary:
    /// <br></br> - <b>Property</b>: Configures how a property of an entity is mapped and stored in the database.
    /// <br></br> - <b>HasOne</b>: Specifies a reference navigation property (one side of a relationship).
    ///<br></br> - <b>WithMany</b>: Specifies a collection navigation property (many side of a relationship).
    ///<br></br> - <b>HasForeignKey</b>: Sets which property is the foreign key in the relationship.
    ///<br></br> - <b>OnDelete</b>: Configures delete behavior (e.g., restrict, cascade) for the relationship.
    ///<br></br> - <b>HasConversion</b>: Configures how a property is stored in the database (e.g., enum as string).
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store the UserRole enum as a string in the database
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // Configure LogEntry to have a required User with a restricted delete behavior
        modelBuilder.Entity<LogEntry>()
            .HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Session to have a required LogEntry with a cascade delete behavior
        modelBuilder.Entity<Session>()
            .HasOne(s => s.LogEntry)
            .WithMany(le => le.Sessions)
            .HasForeignKey(s => s.LogEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
