using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        // Behalte die laufenden Prozesse (PID) im Speicher, um Neustarts/Enden zu erkennen
        private readonly ConcurrentDictionary<int, Session> _activeSessions = new();

        public ProcessMonitorService(IServiceProvider services,
                                     ILogger<ProcessMonitorService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProcessMonitorService gestartet.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Prozessliste holen
                    var processes = Process.GetProcesses();

                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();

                    // 1) Beendete Sessions abschließen
                    foreach (var pid in _activeSessions.Keys.Except(processes.Select(p => p.Id)))
                    {
                        if (_activeSessions.TryRemove(pid, out var session))
                        {
                            session.EndTime = DateTime.Now;
                            session.Duration = session.EndTime - session.StartTime;

                            db.Sessions.Update(session);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Session beendet: {Prog} (PID {Pid}), Dauer {Dur}",
                                                    session.LogEntry.ProgramName,
                                                    pid, session.Duration);
                        }
                    }

                    // 2) Neue oder weiterhin laufende Prozesse verarbeiten
                    foreach (var proc in processes)
                    {
                        // Schau, ob LogEntry für dieses Programm existiert
                        var log = await db.LogEntries
                                          .FirstOrDefaultAsync(
                                            le => le.ProgramName == proc.ProcessName,
                                            stoppingToken);

                        if (log == null)
                        {
                            // Neues Programm entdeckt → LogEntry anlegen
                            log = new LogEntry
                            {
                                ProgramName = proc.ProcessName,
                                FirstSeen = DateTime.Now,
                                UserId = (int)UserRole.System // oder ein fester System-User
                            };
                            db.LogEntries.Add(log);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Neuer LogEntry: {Prog}", proc.ProcessName);
                        }

                        // Existiert schon eine aktive Session?
                        if (!_activeSessions.ContainsKey(proc.Id))
                        {
                            // Neue Session anlegen
                            var session = new Session
                            {
                                LogEntryId = log.LogEntryId,
                                StartTime = DateTime.Now,
                                // EndTime/Duration folgen im Remove-Block
                            };
                            // SessionNumber auf Basis vergangener Sessions
                            var count = await db.Sessions.CountAsync(
                                         s => s.LogEntryId == log.LogEntryId,
                                         stoppingToken);
                            session.SessionNumber = count + 1;

                            db.Sessions.Add(session);
                            await db.SaveChangesAsync(stoppingToken);

                            // Merke die Session im Speicher
                            _activeSessions[proc.Id] = session;

                            _logger.LogInformation("Neue Session #{Num} für {Prog} (PID {Pid})",
                                                    session.SessionNumber,
                                                    proc.ProcessName, proc.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler im ProcessMonitorService");
                }

                // Intervall (z. B. alle 5 Sekunden)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
