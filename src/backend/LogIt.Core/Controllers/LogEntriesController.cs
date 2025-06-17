using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/**
 * @brief API-Controller für Programmeinträge (LogEntries).
 * 
 * - Bietet Endpunkte zum Abrufen und Anlegen von LogEntries.
 * - Zeigt alle oder nur aktive Programmeinträge (mit laufenden Sessions) an.
 */
[ApiController]
[Route("api/[controller]")]
public class LogEntriesController : ControllerBase
{
    /// <summary>
    /// Datenbankkontext für den Zugriff auf LogEntries und Sessions.
    /// </summary>
    private readonly LogItDbContext _db;

    /// <summary>
    /// Erstellt eine neue Instanz des LogEntriesController.
    /// </summary>
    public LogEntriesController(LogItDbContext db) => _db = db;

    /// <summary>
    /// Gibt alle LogEntries zurück, die eine aktive (nicht beendete) Session haben.
    /// </summary>
    /// <returns>
    /// Liste aller LogEntries mit einer offenen Session.
    /// </returns>
    [HttpGet("active")]
    public async Task<IEnumerable<LogEntry>> GetActive() =>
        await _db.LogEntries
            .Include(le => le.Sessions)
            .Where(le => le.Sessions.Any(s => s.EndTime == null))
            .ToListAsync();

    /// <summary>
    /// Gibt alle LogEntries zurück, unabhängig vom Session-Status.
    /// </summary>
    /// <returns>
    /// Liste aller LogEntries mit ihren Sessions.
    /// </returns>
    [HttpGet("all")]
    public async Task<IEnumerable<LogEntry>> GetAll() =>
        await _db.LogEntries
            .Include(le => le.Sessions)
            .ToListAsync();

    /// <summary>
    /// Legt einen neuen LogEntry (Programmeintrag) an.
    /// </summary>
    /// <param name="log">Das anzulegende LogEntry-Objekt.</param>
    /// <returns>
    /// Den angelegten LogEntry mit Status 201 (Created).
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<LogEntry>> Post(LogEntry log)
    {
        /**
        * @brief Setzt das FirstSeen-Datum auf jetzt und speichert den LogEntry.
        */
        log.FirstSeen = DateTime.Now;
        _db.LogEntries.Add(log);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = log.LogEntryId }, log);
    }
}
