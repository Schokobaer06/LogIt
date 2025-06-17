using LogIt.Core.Models;
using System;
using System.Linq;

namespace LogIt.UI.ViewModels
{
    /// <summary>
    /// - ViewModel für die Anzeige eines LogEntry
    /// - Bereitet Daten für die UI auf (z.B. Laufzeit, Status)
    /// </summary>
    public class LogEntryDisplay
    {
        /// <summary>
        /// - Referenz auf das zugehörige LogEntry-Modell
        /// </summary>
        private readonly LogEntry _entry;

        /// <summary>
        /// - Konstruktor
        /// - Setzt das LogEntry, das angezeigt werden soll
        /// </summary>
        /// <param name="entry">LogEntry-Objekt</param>
        public LogEntryDisplay(LogEntry entry)
        {
            _entry = entry;
        }

        /// <summary>
        /// - Name des Programms aus dem LogEntry
        /// </summary>
        public string ProgramName =>
            _entry.ProgramName;

        /// <summary>
        /// - Gibt an, ob das Programm aktuell läuft
        /// - true, wenn eine Session ohne EndTime existiert
        /// </summary>
        public bool IsActive =>
            _entry.Sessions.Any(s => s.EndTime == null);

        /// <summary>
        /// - Schlüssel zum Sortieren (Datum)
        /// - Bei aktiver Session: Startzeit der letzten offenen Session
        /// - Sonst: Endzeit der letzten beendeten Session
        /// </summary>
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

        /// <summary>
        /// - Datum der letzten Nutzung als String
        /// - Bei aktiver Session: Startzeit der offenen Session
        /// - Sonst: Endzeit der letzten Session
        /// </summary>
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
        /// - Gesamtlaufzeit aller Sessions als String
        /// - Addiert alle Session-Dauern
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
        /// - Laufzeit der aktuell offenen Session als String
        /// - Gibt leeren String zurück, wenn keine Session aktiv ist
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

        /// <summary>
        /// - Hilfsmethode zum Formatieren eines TimeSpan als String
        /// - Gibt Stunden/Minuten/Sekunden lesbar aus
        /// </summary>
        /// <param name="ts">Zeitspanne</param>
        /// <returns>Formatierter String</returns>
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
