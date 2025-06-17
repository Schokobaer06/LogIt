namespace LogIt.Core.Models;

/// <summary>
/// Definiert die möglichen Benutzerrollen im System.
/// - Backend: Für serverseitige Aufgaben.
/// - Frontend: Für Benutzeroberfläche/Client.
/// - System: Für Systemprozesse oder interne Aufgaben.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Rolle für Backend-Benutzer (z.B. Serverprozesse).
    /// </summary>
    Backend,
    /// <summary>
    /// Rolle für Frontend-Benutzer (z.B. GUI/Client).
    /// </summary>
    Frontend,
    /// <summary>
    /// Rolle für Systembenutzer (z.B. interne Systemaufgaben).
    /// </summary>
    System
}

/// <summary>
/// Repräsentiert einen Benutzer im System.
/// - Jeder Benutzer hat eine eindeutige ID und eine Rolle.
/// </summary>
public class User
{
    /// <summary>
    /// Eindeutige ID des Benutzers (Primärschlüssel in der Datenbank).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Rolle des Benutzers (Backend, Frontend oder System).
    /// </summary>
    public UserRole Role { get; set; }
}
