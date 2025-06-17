using System;

namespace LogIt.Core.Models;

/// <summary>
/// Repräsentiert eine Programmsitzung (Session) eines überwachten Prozesses.
/// - Speichert Start, Ende, Dauer und Zuordnung zum Programm (LogEntry).
/// </summary>
public class Session
{
    /// <summary>
    /// Eindeutige ID der Session (Primärschlüssel in der Datenbank).
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Zeitpunkt, zu dem die Session (Prozess) gestartet wurde.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Zeitpunkt, zu dem die Session (Prozess) beendet wurde.
    /// <para>
    /// Bleibt <c>null</c>, solange der Prozess noch läuft.
    /// Wird erst gesetzt, wenn der Prozess regulär oder nachträglich beendet wird.
    /// </para>
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Dauer der Session (wird regelmäßig aktualisiert, solange der Prozess läuft).
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Fortlaufende Nummer der Session für das jeweilige Programm (LogEntry).
    /// </summary>
    public int SessionNumber { get; set; }

    /// <summary>
    /// Fremdschlüssel: Verweist auf den zugehörigen LogEntry (Programm).
    /// </summary>
    public int LogEntryId { get; set; }

    /// <summary>
    /// Navigationseigenschaft: Das zugehörige Programm (LogEntry), zu dem diese Session gehört.
    /// Wird aktuell nicht benutzt
    /// </summary>
    public LogEntry LogEntry { get; set; } = default!;
}
