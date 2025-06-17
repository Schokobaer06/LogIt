using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogIt.Core.Data;
using LogIt.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogIt.Core.Services
{
    /// <summary>
    /// Hintergrunddienst, der laufende Prozesse überwacht und deren Start/Ende als Sessions in der Datenbank protokolliert.
    /// - Beendet offene Sessions beim Start.
    /// - Legt neue LogEntries/Sessions für erkannte Programme an.
    /// - Aktualisiert und beendet Sessions automatisch.
    /// </summary>
    public class ProcessMonitorService : BackgroundService
    {
        /// <summary>
        /// Logger für Status- und Fehlerausgaben.
        /// </summary>
        private readonly ILogger<ProcessMonitorService> _logger;

        /// <summary>
        /// ServiceProvider für Dependency Injection (z.B. für DB-Kontext).
        /// </summary>
        private readonly IServiceProvider _services;

        /// <summary>
        /// Hält aktuell laufende Sessions, indexiert nach Prozess-ID.
        /// </summary>
        private readonly ConcurrentDictionary<int, Session> _activeSessions = new();

        /// <summary>
        /// Erstellt eine neue Instanz des ProcessMonitorService.
        /// </summary>
        /// <param name="services">ServiceProvider für Abhängigkeiten.</param>
        /// <param name="logger">Logger für Ausgaben.</param>
        public ProcessMonitorService(IServiceProvider services,
                                     ILogger<ProcessMonitorService> logger)
        {
            _services = services;
            _logger = logger;
        }

        /// <summary>
        /// Hauptlogik des Hintergrunddienstes.
        /// - Beendet offene Sessions beim Start.
        /// - Überwacht Prozesse im 1-Sekunden-Intervall.
        /// - Legt neue Sessions an, aktualisiert und beendet sie.
        /// </summary>
        /// <param name="stoppingToken">Token zum Abbrechen des Dienstes.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProcessMonitorService: Starte Hintergrundüberwachung.");

            /**
             * @brief Offene Sessions beim Start aufräumen.
             * - Findet alle Sessions ohne EndTime und beendet sie nachträglich.
             */
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();

                // Finde alle Sessions, deren EndTime null ist (nie regulär beendet)
                var openSessions = await db.Sessions
                                           .Include(s => s.LogEntry)
                                           .Where(s => s.EndTime == null)
                                           .ToListAsync(stoppingToken);

                if (openSessions.Count > 0)
                {
                    _logger.LogWarning(
                        "{Count} offene Session(s) gefunden. Beende sie jetzt.",
                        openSessions.Count);

                    foreach (var session in openSessions)
                    {
                        /**
                         * @brief Setzt EndTime für alte offene Sessions.
                         * - Falls Duration > 0: EndTime = StartTime + Duration
                         * - Sonst: EndTime = StartTime
                         */
                        if (session.Duration > TimeSpan.Zero)
                        {
                            session.EndTime = session.StartTime.Add(session.Duration);
                        }
                        else
                        {
                            session.EndTime = session.StartTime;
                        }

                        db.Sessions.Update(session);

                        _logger.LogInformation(
                            "Session #{SessionNumber} für '{Prog}' wurde nachträglich beendet. " +
                            "Start={Start}, End={End}, Dauer={Dur}",
                            session.SessionNumber,
                            session.LogEntry.ProgramName,
                            session.StartTime,
                            session.EndTime,
                            session.Duration);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Aufräumen alter Sessions abgeschlossen.");
                }
                else
                {
                    _logger.LogInformation("Keine offenen Sessions beim Start gefunden.");
                }
            }

            // --- Hauptüberwachungsschleife ---
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processes = Process.GetProcesses();

                    using var scope2 = _services.CreateScope();
                    var db2 = scope2.ServiceProvider.GetRequiredService<LogItDbContext>();

                    /**
                     * @brief Beendet Sessions, deren Prozesse nicht mehr laufen.
                     * - Setzt EndTime und berechnet Dauer.
                     */
                    foreach (var pid in _activeSessions.Keys.Except(processes.Select(p => p.Id)))
                    {
                        if (_activeSessions.TryRemove(pid, out var session))
                        {
                            session.EndTime = DateTime.Now;
                            session.Duration = session.EndTime.Value - session.StartTime;
                            db2.Sessions.Update(session);
                            await db2.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Session #{SessionNumber} für '{Prog}' (PID {Pid}) regulär beendet. Dauer={Dur}",
                                session.SessionNumber,
                                session.LogEntry.ProgramName,
                                pid,
                                session.Duration);
                        }
                    }

                    /**
                     * @brief Aktualisiert die Dauer aller laufenden Sessions.
                     */
                    foreach (var kv in _activeSessions)
                    {
                        var pid = kv.Key;
                        var session = kv.Value;

                        var newDuration = DateTime.Now - session.StartTime;
                        if (newDuration > session.Duration)
                        {
                            session.Duration = newDuration;
                            db2.Sessions.Update(session);
                        }
                    }
                    await db2.SaveChangesAsync(stoppingToken);

                    /**
                     * @brief Prüft alle laufenden Prozesse und legt ggf. neue Sessions/LogEntries an.
                     */
                    foreach (var proc in processes)
                    {
                        // 3a) GUI-Filter: Nur sichtbare Fenster, nicht aus Windows-Ordner
                        bool isGuiApp = false;
                        try
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero)
                                isGuiApp = true;

                            var path = proc.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(path))
                            {
                                var folder = Path.GetDirectoryName(path) ?? string.Empty;
                                if (folder.StartsWith(
                                        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    isGuiApp = false;
                                }
                            }
                        }
                        catch
                        {
                            isGuiApp = false;
                        }
                        if (!isGuiApp) continue;

                        // Loggt nicht das Frontend (LogIt.UI)
                        string? processDescription;
                        try
                        {
                            processDescription = proc.MainModule?.FileVersionInfo.FileDescription;
                        }
                        catch
                        {
                            processDescription = null;
                        }

                        if (string.Equals(proc.ProcessName, "LogIt.UI", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(processDescription, "LogIt.UI", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // 3b) Suche existierenden LogEntry (nach ProgramName)
                        string? programName;
                        try
                        {
                            programName = proc.MainModule?.FileVersionInfo.FileDescription;
                            if (string.IsNullOrWhiteSpace(programName))
                                programName = proc.ProcessName;
                        }
                        catch
                        {
                            programName = proc.ProcessName;
                        }

                        var log = await db2.LogEntries
                                           .Include(le => le.Sessions)
                                           .FirstOrDefaultAsync(
                                             le => le.ProgramName == programName,
                                             stoppingToken);

                        if (log == null)
                        {
                            /**
                             * @brief Legt neuen LogEntry für unbekanntes Programm an.
                             */
                            log = new LogEntry
                            {
                                ProgramName = programName,
                                FirstSeen = DateTime.Now,
                                UserId = (int)UserRole.System
                            };
                            db2.LogEntries.Add(log);
                            await db2.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Neuer LogEntry angelegt: '{Prog}' (erste Sichtung={Time})",
                                log.ProgramName,
                                log.FirstSeen);
                        }

                        // 3c) Prüfe, ob bereits eine Session für diesen PID existiert
                        if (!_activeSessions.ContainsKey(proc.Id))
                        {
                            /**
                             * @brief Legt neue Session für laufenden Prozess an.
                             */
                            var session = new Session
                            {
                                LogEntryId = log.LogEntryId,
                                StartTime = DateTime.Now,
                                Duration = TimeSpan.Zero,
                                SessionNumber = await db2.Sessions.CountAsync(
                                                   s => s.LogEntryId == log.LogEntryId,
                                                   stoppingToken) + 1
                            };

                            db2.Sessions.Add(session);
                            await db2.SaveChangesAsync(stoppingToken);

                            _activeSessions[proc.Id] = session;

                            _logger.LogInformation(
                                "Neue Session #{SessionNumber} gestartet für '{Prog}' (PID {Pid}) um {Time}",
                                session.SessionNumber,
                                log.ProgramName,
                                proc.Id,
                                session.StartTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    /**
                     * @brief Fehlerbehandlung für Prozess- und Datenbankoperationen.
                     */
                    _logger.LogError(ex,
                        "Fehler im ProcessMonitorService bei DB-Operationen oder Prozessabfrage.");
                }

                // Intervall: 1 Sekunde warten
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }

            _logger.LogInformation("ProcessMonitorService: Beende Hintergrundüberwachung.");
        }
    }
}
