using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly LogItDbContext _db;
    public UsersController(LogItDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<User>> Get() =>
        await _db.Users.ToListAsync();

    [HttpPost]
    public async Task<ActionResult<User>> Post(User u)
    {
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = u.UserId }, u);
    }
}
