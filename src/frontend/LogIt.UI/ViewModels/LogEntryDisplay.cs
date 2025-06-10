using LogIt.Core.Models;
using System;
using System.Linq;

namespace LogIt.UI.ViewModels
{
    public class LogEntryDisplay
    {
        private readonly LogEntry _entry;

        public LogEntryDisplay(LogEntry entry)
        {
            _entry = entry;
        }

        public string ProgramName =>
            _entry.ProgramName;

        public bool IsActive =>
            _entry.Sessions.Any(s => s.EndTime == null);

        public DateTime SortKey
        {
            get
            {
                if (IsActive)
                    return _entry.Sessions.Where(s => s.EndTime == null)
                                          .Max(s => s.StartTime);
                else
                    return _entry.Sessions.Max(s => s.EndTime!.Value);
            }
        }

        public string LastUsedDisplay
        {
            get
            {
                if (IsActive)
                {
                    var running = _entry.Sessions.First(s => s.EndTime == null);
                    return running.StartTime.ToString("dd.MM.yyyy");
                }
                else
                {
                    var lastEnded = _entry.Sessions.Max(s => s.EndTime!.Value);
                    return lastEnded.ToString("dd.MM.yyyy");
                }
            }
        }

        /// <summary>
        /// Insgesamt gelaufene Zeit = Summe aller Duration
        /// </summary>
        public string TotalRunTimeDisplay
        {
            get
            {
                // Summe aller Ticks
                var totalTicks = _entry.Sessions
                    .Select(s => s.Duration.Ticks)
                    .Sum();
                var total = TimeSpan.FromTicks(totalTicks);
                return FormatTimeSpan(total);
            }
        }

        /// <summary>
        /// Aktuell laufende Zeit = Duration der offenen Session oder leer
        /// </summary>
        public string CurrentRunTimeDisplay
        {
            get
            {
                if (IsActive)
                {
                    var running = _entry.Sessions.First(s => s.EndTime == null);
                    return FormatTimeSpan(running.Duration);
                }
                return string.Empty;
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
}
