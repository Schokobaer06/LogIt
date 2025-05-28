namespace LogIt.Core.Models;

public class LogEntry
{
    public int LogEntryId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }

    // Navigation
    public int UserId { get; set; }
    public User? User { get; set; }

    public List<Session> Sessions { get; set; } = new();
}
