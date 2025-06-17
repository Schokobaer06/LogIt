using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core.Controllers;

/**
 * @brief API-Controller für Benutzerverwaltung.
 * 
 * - Bietet Endpunkte zum Abrufen und Anlegen von Benutzern.
 * - Nutzt Entity Framework Core für Datenbankzugriffe.
 */
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
    public UsersController(LogItDbContext db) => _db = db;

    /// <summary>
    /// Gibt eine Liste aller Benutzer zurück.
    /// </summary>
    /// <returns>Alle Benutzer aus der Datenbank.</returns>
    [HttpGet]
    public async Task<IEnumerable<User>> Get() =>
        await _db.Users.ToListAsync();

    /// <summary>
    /// Legt einen neuen Benutzer an.
    /// </summary>
    /// <param name="u">Der anzulegende Benutzer.</param>
    /// <returns>Den angelegten Benutzer mit Status 201 (Created).</returns>
    [HttpPost]
    public async Task<ActionResult<User>> Post(User u)
    {
        /**
        * @brief Fügt den Benutzer zur Datenbank hinzu und speichert die Änderung.
        */
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = u.UserId }, u);
    }
}
