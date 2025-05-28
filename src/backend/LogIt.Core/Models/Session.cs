namespace LogIt.Core.Models;

public class Session
{
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int SessionNumber { get; set; }

    // Navigation
    public int LogEntryId { get; set; }
    public LogEntry? LogEntry { get; set; }
}
