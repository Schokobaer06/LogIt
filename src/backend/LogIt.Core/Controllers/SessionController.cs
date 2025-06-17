using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/**
 * @brief API-Controller für Sitzungen (Sessions) eines bestimmten Programms (LogEntry).
 * 
 * - Erlaubt das Anlegen neuer Sessions für ein LogEntry.
 * - Nutzt Entity Framework Core für Datenbankzugriffe.
 */
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
    [HttpPost]
    public async Task<ActionResult<Session>> Post(int logId, Session session)
    {
        var log = await _db.LogEntries.FindAsync(logId);
        if (log == null) return NotFound();

        /**
         * @brief Setzt Session-Nummer und LogEntryId für die neue Session.
         * - SessionNumber: fortlaufende Nummer für dieses Programm.
         * - LogEntryId: Verknüpft die Session mit dem LogEntry.
         */
        session.SessionNumber = await _db.Sessions.CountAsync(s => s.LogEntryId == logId) + 1;
        session.LogEntryId = logId;

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Post), new { logId, id = session.SessionId }, session);
    }
}
