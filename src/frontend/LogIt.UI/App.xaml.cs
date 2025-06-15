using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using IWshRuntimeLibrary; // Oben ergänzen
using System.Runtime.InteropServices; // Für COMException

namespace LogIt.UI
{
    public partial class App : System.Windows.Application
    {
        private Process? _backendProcess;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartBackendIfNeeded();
            RegisterInStartup();
            CreateStartMenuShortcut();
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
                System.Windows.MessageBox.Show(
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

        private void RegisterInStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null)
                {
                    // Handle the case where the registry key could not be opened
                    return;
                }

                var exePath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");

                // Set the registry entry to start the app at login
                key.SetValue("LogIt", $"\"{exePath}\" --minimized");
            }
            catch
            {
                // Silently ignore or log the error
            }
        }

        public static void CreateStartMenuShortcut()
        {
            // Pfad zur ausführbaren Datei
            var exePath = Path.Combine(
                AppContext.BaseDirectory,        // oder dort, wo deine EXE liegt
                "LogIt.UI.exe"
            );

            if (!System.IO.File.Exists(exePath))
                throw new FileNotFoundException("EXE nicht gefunden", exePath);

            // Pfad zum Benutzer‑Startmenü\Programme
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            // Eine eigene Untergruppe wäre z.B. "LogIt"
            var shortcutFolder = Path.Combine(programsFolder, "LogIt");
            Directory.CreateDirectory(shortcutFolder);

            // Verknüpfungsdatei
            var lnkPath = Path.Combine(shortcutFolder, "LogIt.lnk");

            var shell = new WshShell();
            // COM‑Objekt erzeugen
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkPath);

            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Arguments = "--minimized";                // falls du Start‑Argumente brauchst
            shortcut.Description = "LogIt – Process Logger";
            shortcut.IconLocation = exePath + ",0";            // Icon aus EXE nehmen
            shortcut.Save();
        }
    }
}
