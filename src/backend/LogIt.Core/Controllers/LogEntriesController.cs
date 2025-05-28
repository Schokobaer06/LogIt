using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogEntriesController : ControllerBase
{
    private readonly LogItDbContext _db;
    public LogEntriesController(LogItDbContext db) => _db = db;

    [HttpGet("active")]
    public async Task<IEnumerable<LogEntry>> GetActive() =>
        await _db.LogEntries
                 .Include(le => le.Sessions)
                 .Where(le => le.Sessions.Any(s => s.EndTime > s.StartTime) == false)
                 .ToListAsync();

    [HttpGet("all")]
    public async Task<IEnumerable<LogEntry>> GetAll() =>
        await _db.LogEntries.Include(le => le.Sessions).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<LogEntry>> Post(LogEntry log)
    {
        log.FirstSeen = DateTime.Now;
        _db.LogEntries.Add(log);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = log.LogEntryId }, log);
    }
}
