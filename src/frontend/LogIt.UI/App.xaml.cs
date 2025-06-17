using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using IWshRuntimeLibrary; // Für Startmenü-Verknüpfung
using System.Runtime.InteropServices; // Für COMException

namespace LogIt.UI
{
    /// <summary>
    /// Hauptanwendungsklasse für LogIt.
    /// - Startet das Backend automatisch, falls nötig
    /// - Erstellt Startmenü-Verknüpfung
    /// - (Optional/experimentell) Registrierung für Autostart
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// Referenz auf den gestarteten Backend-Prozess
        /// </summary>
        private Process? _backendProcess;

        /// <summary>
        /// Wird beim Starten der Anwendung aufgerufen.
        /// - Startet Backend, falls nicht vorhanden
        /// - Erstellt Startmenü-Verknüpfung
        /// - (Optional) Registriert Autostart (auskommentiert)
        /// </summary>
        /// <param name="e">Start-Event-Argumente</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartBackendIfNeeded();
            //RegisterInStartup();
            CreateStartMenuShortcut();
        }

        /// <summary>
        /// Startet das Backend, falls es noch nicht läuft.
        /// - Prüft, ob der Prozess existiert
        /// - Startet die EXE im Unterordner "Backend"
        /// - Zeigt Fehlermeldung, falls Backend nicht gefunden oder nicht startbar
        /// </summary>
        private void StartBackendIfNeeded()
        {
            const string backendName = "LogIt.Core";

            // Prüfen, ob Backend-Prozess schon läuft
            var existing = Process.GetProcessesByName(backendName);
            if (existing.Length > 0) return;

            // Pfad zur Backend-EXE
            var exePath = Path.Combine(
                AppContext.BaseDirectory,
                "Backend",
                "LogIt.Core.exe"
            );

            if (!System.IO.File.Exists(exePath))
            {
                System.Windows.MessageBox.Show(
                    $"Konnte Backend-Executable nicht finden: {exePath}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // Backend-Prozess starten
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
                System.Windows.MessageBox.Show(
                    $"Fehler beim Starten des Backends:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Wird beim Beenden der Anwendung aufgerufen.
        /// - Beendet das Backend, falls es von der UI gestartet wurde
        /// </summary>
        /// <param name="e">Exit-Event-Argumente</param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Backend-Prozess ggf. beenden
            try
            {
                if (_backendProcess != null && !_backendProcess.HasExited)
                {
                    _backendProcess.Kill();
                }
            }
            catch
            {
                // Fehler beim Beenden werden ignoriert
            }
        }

        /// <summary>
        /// (Experimentell, NICHT korrekt funktionierend!)
        /// Registriert die Anwendung für den Windows-Autostart.
        /// - Schreibt Registry-Eintrag für Autostart
        /// - Startet die App beim Login minimiert
        /// - Funktioniert aktuell nicht zuverlässig!
        /// </summary>
        private void RegisterInStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null)
                    return;

                // Pfad zur laufenden EXE
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;

                if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return;

                // Registry-Eintrag setzen
                key.SetValue("LogIt", $"\"{exePath}\" --minimized");
            }
            catch
            {
                // Fehler werden ignoriert
            }
        }

        /// <summary>
        /// Erstellt eine Verknüpfung im Startmenü für LogIt.
        /// - Ziel: LogIt.UI.exe im aktuellen Verzeichnis
        /// - Legt Verknüpfung im Startmenü-Ordner "LogIt" an; Aufrufbar übers Startmenü, aber das Programm muss mindstens einmal gestartet werden, damit die Verknüpfung existiert.
        /// </summary>
        public static void CreateStartMenuShortcut()
        {
            // Pfad zur EXE
            var exePath = Path.Combine(
                AppContext.BaseDirectory,
                "LogIt.UI.exe"
            );

            if (!System.IO.File.Exists(exePath))
                throw new FileNotFoundException("EXE nicht gefunden", exePath);

            // Startmenü-Ordner
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var shortcutFolder = Path.Combine(programsFolder, "LogIt");
            Directory.CreateDirectory(shortcutFolder);

            // Pfad zur Verknüpfung
            var lnkPath = Path.Combine(shortcutFolder, "LogIt.lnk");

            var shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkPath);

            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Arguments = "";
            shortcut.Description = "LogIt – Process Logger";
            shortcut.IconLocation = exePath + ",0";
            shortcut.Save();
        }
    }
}
