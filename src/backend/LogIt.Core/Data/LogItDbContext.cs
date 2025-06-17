using LogIt.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Data;

/**
 * @brief Entity Framework Core database context for the LogIt application.
 * 
 * - Verwaltet Zugriff auf User, LogEntries und Sessions.
 * - Konfiguriert Beziehungen und Speicherung der Entitäten.
 */
public class LogItDbContext : DbContext
{
    /// <summary>
    /// Erstellt eine neue Instanz des <see cref="LogItDbContext"/> mit den angegebenen Optionen.
    /// </summary>
    /// <param name="options">Optionen für die Datenbankverbindung und das Verhalten des Contexts.</param>
    public LogItDbContext(DbContextOptions<LogItDbContext> options)
        : base(options) { }

    /// <summary>
    /// Stellt die Tabelle für Benutzer (User) bereit.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Stellt die Tabelle für Log-Einträge (LogEntry) bereit.
    /// </summary>
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    /// <summary>
    /// Stellt die Tabelle für Sitzungen (Session) bereit.
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// Konfiguriert die Beziehungen und Eigenschaften der Entitäten.
    /// <para>
    /// - Speichert UserRole als String in der Datenbank.<br/>
    /// - LogEntry benötigt einen User (Löschen von User ist eingeschränkt).<br/>
    /// - Session benötigt einen LogEntry (Löschen von LogEntry löscht zugehörige Sessions).
    /// </para>
    /// <para>
    /// <b>Property</b>: Legt fest, wie eine Eigenschaft gespeichert wird.<br/>
    /// <b>HasOne</b>: Definiert eine Referenz (eine Seite der Beziehung).<br/>
    /// <b>WithMany</b>: Definiert eine Sammlung (viele Seite der Beziehung).<br/>
    /// <b>HasForeignKey</b>: Legt den Fremdschlüssel fest.<br/>
    /// <b>OnDelete</b>: Legt das Löschverhalten fest (Restrict, Cascade).<br/>
    /// <b>HasConversion</b>: Speichert z.B. Enum als String.
    /// </para>
    /// </summary>
    /// <param name="modelBuilder">Hilfsklasse zum Konfigurieren des Datenmodells.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /**
         * @brief Speichert das UserRole-Enum als String in der Datenbank.
         */
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        /**
         * @brief LogEntry benötigt einen User, Löschen ist eingeschränkt (Restrict).
         */
        modelBuilder.Entity<LogEntry>()
            .HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        /**
         * @brief Session benötigt einen LogEntry, Löschen ist kaskadiert (Cascade).
         */
        modelBuilder.Entity<Session>()
            .HasOne(s => s.LogEntry)
            .WithMany(le => le.Sessions)
            .HasForeignKey(s => s.LogEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
