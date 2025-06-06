using LogIt.Core.Models;
using System;
using System.Linq;

namespace LogIt.UI.ViewModels
{
    public class LogEntryDisplay
    {
        private readonly LogEntry _entry;

        public LogEntry Entry => _entry;

        public LogEntryDisplay(LogEntry entry)
        {
            _entry = entry;
        }

        /// <summary>
        /// Programm‐Name
        /// </summary>
        public string ProgramName => _entry.ProgramName;

        /// <summary>
        /// True, wenn eine Session ohne EndTime existiert → läuft gerade
        /// </summary>
        public bool IsActive =>
            _entry.Sessions != null && _entry.Sessions.Any(s => s.EndTime == null);

        /// <summary>
        /// Sortierschlüssel: Bei aktiv → StartTime der laufenden Session, 
        /// sonst → zuletzt beendete EndTime
        /// </summary>
        public DateTime SortKey
        {
            get
            {
                if (IsActive)
                {
                    return _entry.Sessions
                                 .Where(s => s.EndTime == null)
                                 .Max(s => s.StartTime);
                }
                else
                {
                    return _entry.Sessions.Max(s => s.EndTime.Value);
                }
            }
        }

        /// <summary>
        /// Anzeige‐Text: „Läuft seit …“ für aktive oder EndTime für inaktive
        /// </summary>
        public string LastUsedDisplay
        {
            get
            {
                if (IsActive)
                {
                    var running = _entry.Sessions.First(s => s.EndTime == null);
                    return $"Läuft seit {running.StartTime:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    var lastEnded = _entry.Sessions.Max(s => s.EndTime.Value);
                    return lastEnded.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
        }
    }
}
