using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace LogIt.UI
{
    public partial class App : Application
    {
        private Process? _backendProcess;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartBackendIfNeeded();
        }

        private void StartBackendIfNeeded()
        {
            // Prozessname ohne Extension
            const string backendName = "LogIt.Core";

            // 1) Prüfen, ob der Prozess schon läuft
            var existing = Process.GetProcessesByName(backendName);
            if (existing.Length > 0) return;

            // 2) Pfad zur Backend-EXE (im UI-Output-Ordner unter "Backend")
            var exePath = Path.Combine(
                AppContext.BaseDirectory,
                "Backend",
                "LogIt.Core.exe"
            );

            if (!File.Exists(exePath))
            {
                MessageBox.Show(
                    $"Konnte Backend-Executable nicht finden: {exePath}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // 3) Starten des Backend-Prozesses
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _backendProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Starten des Backends:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Optional: Beim Schließen der UI das Backend auch beenden
            try
            {
                if (_backendProcess != null && !_backendProcess.HasExited)
                {
                    _backendProcess.Kill();
                }
            }
            catch
            {
                // Falls das Schließen fehlschlägt, ignoriere
            }
        }
    }
}
