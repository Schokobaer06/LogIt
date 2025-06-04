using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

[ApiController] 
[Route("api/logentries/{logId}/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly LogItDbContext _db;
    public SessionsController(LogItDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<Session>> Post(int logId, Session session)
    {
        var log = await _db.LogEntries.FindAsync(logId);
        if (log == null) return NotFound();

        //session.Duration = session.EndTime - session.StartTime;
        session.SessionNumber = await _db.Sessions.CountAsync(s => s.LogEntryId == logId) + 1;
        session.LogEntryId = logId;

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Post), new { logId, id = session.SessionId }, session);
    }
}
