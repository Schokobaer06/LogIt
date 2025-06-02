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
    public class ProcessMonitorService : BackgroundService
    {
        private readonly ILogger<ProcessMonitorService> _logger;
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<int, Session> _activeSessions = new();

        public ProcessMonitorService(IServiceProvider services,
                                     ILogger<ProcessMonitorService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProcessMonitorService: Starte Hintergrundüberwachung.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1) Alle aktuell laufenden Prozesse abfragen
                    var processes = Process.GetProcesses();

                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();

                    // 2) Beendete Sessions abschließen
                    foreach (var pid in _activeSessions.Keys.Except(processes.Select(p => p.Id)))
                    {
                        if (_activeSessions.TryRemove(pid, out var session))
                        {
                            session.EndTime = DateTime.Now;
                            session.Duration = session.EndTime - session.StartTime;
                            db.Sessions.Update(session);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Session #{SessionNumber} für Programm '{ProgName}' (PID {Pid}) beendet. Dauer: {Dur}",
                                session.SessionNumber,
                                session.LogEntry.ProgramName,
                                pid,
                                session.Duration);
                        }
                    }

                    // 3) Neue oder weiterlaufende Prozesse prüfen
                    foreach (var proc in processes)
                    {
                        // --- Filter: überspringe Systemprozesse und Hintergrunddienste ---
                        bool isGuiApp = false;
                        try
                        {
                            // a) Prüfe, ob Hauptfenster vorhanden ist
                            if (proc.MainWindowHandle != IntPtr.Zero)
                                isGuiApp = true;

                            // b) Optional: Dateipfad anschauen und Windows-Verzeichnis ausschließen
                            var path = proc.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(path))
                            {
                                var folder = Path.GetDirectoryName(path) ?? string.Empty;
                                if (folder.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                                      StringComparison.OrdinalIgnoreCase))
                                {
                                    isGuiApp = false;
                                }
                            }
                        }
                        catch
                        {
                            // Zugriff auf MainModule kann fehlschlagen (z.B. Systemprozesse)
                            isGuiApp = false;
                        }

                        if (!isGuiApp)
                        {
                            // System- oder Hintergrundprozess, überspringen
                            continue;
                        }

                        // --- Filterende, nur GUI-Anwendungen laufen weiter ---

                        // 4) Suche nach vorhandenem LogEntry (nach ProgramName)
                        var programName = proc.ProcessName;
                        var log = await db.LogEntries
                                          .Include(le => le.Sessions)
                                          .FirstOrDefaultAsync(
                                            le => le.ProgramName == programName,
                                            stoppingToken);

                        if (log == null)
                        {
                            // 5) Neues GUI-Programm erkannt → LogEntry anlegen
                            log = new LogEntry
                            {
                                ProgramName = programName,
                                FirstSeen = DateTime.Now,
                                UserId = (int)UserRole.System
                            };
                            db.LogEntries.Add(log);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Neuer LogEntry angelegt: '{ProgName}' (erste Sichtung: {Time})",
                                log.ProgramName,
                                log.FirstSeen);
                        }

                        // 6) Prüfe, ob bereits eine Session für diesen PID existiert
                        if (!_activeSessions.ContainsKey(proc.Id))
                        {
                            // Neue Session starten
                            var session = new Session
                            {
                                LogEntryId = log.LogEntryId,
                                StartTime = DateTime.Now,
                                SessionNumber = await db.Sessions.CountAsync(
                                                   s => s.LogEntryId == log.LogEntryId,
                                                   stoppingToken) + 1
                            };

                            db.Sessions.Add(session);
                            await db.SaveChangesAsync(stoppingToken);

                            _activeSessions[proc.Id] = session;

                            _logger.LogInformation(
                                "Neue Session #{SessionNumber} gestartet für Programm '{ProgName}' (PID {Pid}) um {Time}",
                                session.SessionNumber,
                                log.ProgramName,
                                proc.Id,
                                session.StartTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 7) Fehler protokollieren
                    _logger.LogError(ex,
                        "Fehler im ProcessMonitorService bei Datenbank-Operationen oder Prozessabfrage.");
                }

                // 8) Kurze Pause vor der nächsten Iteration
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }

            _logger.LogInformation("ProcessMonitorService: Beende Hintergrundüberwachung.");
        }
    }
}
