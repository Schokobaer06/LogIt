using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/// <summary>
/// API-Controller für Sitzungen (Sessions) eines bestimmten Programms (LogEntry).
/// <para>
/// - Erlaubt das Anlegen neuer Sessions für ein LogEntry.
/// - Nutzt Entity Framework Core für Datenbankzugriffe.
/// </para>
/// </summary>
[ApiController]
[Route("api/logentries/{logId}/[controller]")]
public class SessionsController : ControllerBase
{
    /// <summary>
    /// Datenbankkontext für den Zugriff auf Sessions und LogEntries.
    /// </summary>
    private readonly LogItDbContext _db;
    /// <summary>
    /// Erstellt eine neue Instanz des SessionsController.
    /// </summary>
    /// <param name="db">Datenbankkontext</param>
    public SessionsController(LogItDbContext db) => _db = db;

    /// <summary>
    /// Legt eine neue Session für ein bestimmtes LogEntry (Programm) an.
    /// </summary>
    /// <param name="logId">ID des LogEntry (Programms), zu dem die Session gehört.</param>
    /// <param name="session">Session-Objekt mit Start-/Endzeit und weiteren Daten.</param>
    /// <returns>
    /// Die angelegte Session mit Status 201 (Created), 
    /// oder 404 (NotFound), falls das LogEntry nicht existiert.
    /// </returns>
    /// <response code="201">Session erfolgreich angelegt</response>
    /// <response code="404">Kein LogEntry mit der angegebenen ID gefunden</response>
    [HttpPost]
    [ProducesResponseType(typeof(Session), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Session>> Post(int logId, Session session)
    {
        var log = await _db.LogEntries.FindAsync(logId);
        if (log == null)
            return NotFound();

        // Setzt Session-Nummer und LogEntryId für die neue Session.
        session.SessionNumber = await _db.Sessions.CountAsync(s => s.LogEntryId == logId) + 1;
        session.LogEntryId = logId;

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Post), new { logId, id = session.SessionId }, session);
    }
}
