using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/// <summary>
/// API-Controller für Benutzerverwaltung.
/// <para>
/// Bietet Endpunkte zum Abrufen und Anlegen von Benutzern.
/// Nutzt Entity Framework Core für Datenbankzugriffe.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Datenbankkontext für den Zugriff auf Benutzer.
    /// </summary>
    private readonly LogItDbContext _db;

    /// <summary>
    /// Erstellt eine neue Instanz des UsersController.
    /// </summary>
    /// <param name="db">Datenbankkontext</param>
    public UsersController(LogItDbContext db) => _db = db;

    /// <summary>
    /// Gibt eine Liste aller Benutzer zurück.
    /// </summary>
    /// <returns>Alle Benutzer aus der Datenbank.</returns>
    /// <response code="200">Erfolgreich, gibt alle Benutzer zurück</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<User>> Get() =>
        await _db.Users.ToListAsync();

    /// <summary>
    /// Legt einen neuen Benutzer an.
    /// </summary>
    /// <param name="u">Der anzulegende Benutzer.</param>
    /// <returns>Den angelegten Benutzer mit Status 201 (Created).</returns>
    /// <response code="201">Benutzer erfolgreich angelegt</response>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    public async Task<ActionResult<User>> Post(User u)
    {
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = u.UserId }, u);
    }
}
