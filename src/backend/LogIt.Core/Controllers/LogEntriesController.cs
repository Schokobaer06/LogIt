using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/// <summary>
/// API-Controller für Programmeinträge (LogEntries).
/// <para>
/// Bietet Endpunkte zum Abrufen und Anlegen von LogEntries.
/// Zeigt alle oder nur aktive Programmeinträge (mit laufenden Sessions) an.
/// </para>
/// </summary>
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
    /// <param name="db">Datenbankkontext</param>
    public LogEntriesController(LogItDbContext db) => _db = db;

    /// <summary>
    /// Gibt alle LogEntries zurück, die eine aktive (nicht beendete) Session haben.
    /// </summary>
    /// <returns>Liste aller LogEntries mit mindestens einer offenen Session.</returns>
    /// <response code="200">Erfolgreich, gibt alle aktiven LogEntries zurück</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<LogEntry>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<LogEntry>> GetActive() =>
        await _db.LogEntries
            .Include(le => le.Sessions)
            .Where(le => le.Sessions.Any(s => s.EndTime == null))
            .ToListAsync();

    /// <summary>
    /// Gibt alle LogEntries zurück, unabhängig vom Session-Status.
    /// </summary>
    /// <returns>Liste aller LogEntries mit ihren Sessions.</returns>
    /// <response code="200">Erfolgreich, gibt alle LogEntries zurück</response>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<LogEntry>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<LogEntry>> GetAll() =>
        await _db.LogEntries
            .Include(le => le.Sessions)
            .ToListAsync();

    /// <summary>
    /// Legt einen neuen LogEntry (Programmeintrag) an.
    /// </summary>
    /// <param name="log">Das anzulegende LogEntry-Objekt.</param>
    /// <returns>Den angelegten LogEntry mit Status 201 (Created).</returns>
    /// <response code="201">LogEntry erfolgreich angelegt</response>
    [HttpPost]
    [ProducesResponseType(typeof(LogEntry), StatusCodes.Status201Created)]
    public async Task<ActionResult<LogEntry>> Post(LogEntry log)
    {
        log.FirstSeen = DateTime.Now;
        _db.LogEntries.Add(log);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = log.LogEntryId }, log);
    }
}
