namespace LogIt.Core.Models;

/// <summary>
/// Repräsentiert einen Programmeintrag (LogEntry) in der Datenbank.
/// - Speichert Informationen zu einem überwachten Programm/Prozess.
/// - Hält die Historie aller zugehörigen Sessions (Laufzeiten).
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Eindeutige ID des LogEntry (Primärschlüssel in der Datenbank).
    /// </summary>
    public int LogEntryId { get; set; }

    /// <summary>
    /// Name oder Beschreibung des überwachten Programms/Prozesses.
    /// </summary>
    public string ProgramName { get; set; } = string.Empty;

    /// <summary>
    /// Zeitpunkt, zu dem das Programm zum ersten Mal erkannt wurde.
    /// </summary>
    public DateTime FirstSeen { get; set; }

    /// <summary>
    /// Fremdschlüssel: Verweist auf den zugehörigen Benutzer (User), der das Programm ausführt.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigationseigenschaft: Der Benutzer (User), dem dieser LogEntry zugeordnet ist.
    /// Wird aktuell nicht benutzt
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigationseigenschaft: Liste aller Sessions (Laufzeiten) dieses Programms.
    /// </summary>
    public List<Session> Sessions { get; set; } = [];
}
