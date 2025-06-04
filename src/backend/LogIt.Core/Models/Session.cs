namespace LogIt.Core.Models;

public class Session
{
    public int SessionId { get; set; }

    public DateTime StartTime { get; set; }

    // EndTime bleibt nullable, wird erst gesetzt, wenn wir den Prozess normal beenden
    public DateTime? EndTime { get; set; }

    // In der DB speichern wir Duration als TimeSpan (upgedated jede Sekunde)
    public TimeSpan Duration { get; set; }

    public int SessionNumber { get; set; }

    // Fremdschlüssel
    public int LogEntryId { get; set; }
    public LogEntry LogEntry { get; set; } = default!;
}
