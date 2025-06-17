using System.ComponentModel;
using System.IO.Packaging;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;     
using System.Drawing;

namespace LogIt.UI
{
    /// <summary>
    /// Hauptfenster der Anwendung.
    /// - Stellt die Hauptoberfläche dar
    /// - Unterstützt Minimieren in den System-Tray
    /// - Verarbeitet Fensterereignisse und Tray-Interaktionen
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// - Icon im System-Tray (Benachrichtigungsbereich)
        /// </summary>
        private NotifyIcon? _notifyIcon;

        /// <summary>
        /// - Gibt an, ob das Fenster beim Schließen minimiert werden soll
        /// </summary>
        private bool _minimizeOnClose = false;

        /// <summary>
        /// Konstruktor.
        /// - Initialisiert UI, DataContext und Tray-Icon
        /// - Setzt Event-Handler für Fenster und Tray
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainWindowViewModel();

            // Checkbox-Event für "Minimieren beim Schließen"
            MinimizeOnCloseCheckBox.Checked += (s, e) => _minimizeOnClose = true;
            MinimizeOnCloseCheckBox.Unchecked += (s, e) => _minimizeOnClose = false;

            // Icon für System-Tray
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("app.ico"),
                Visible = false,
                Text = "LogIt"
            };

            // Context-Menu für  Tray-Icon
            var menu = new ContextMenuStrip();
            menu.Items.Add("Öffnen", null, (_, __) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.WindowState = WindowState.Normal;
                    this.ShowInTaskbar = true;
                    if (_notifyIcon != null)
                        _notifyIcon.Visible = false;
                    this.Activate();
                });
            });
            menu.Items.Add("Beenden", null, (_, __) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    // Event-Handler entfernen
                    this.Closing -= MainWindow_Closing;
                    this.Close();
                });
            });
            _notifyIcon.ContextMenuStrip = menu;

            // Doppelklick auf Tray-Icon öffnet das Fenster
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Startet minimiert, wenn "--minimized" als Argument übergeben wurde
            var arg = Environment.GetCommandLineArgs();
            if (arg.Contains("--minimized"))
            {
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                _notifyIcon.Visible = true;
            }

            // Fenster-Events abonnieren
            this.Closing += MainWindow_Closing;
            this.StateChanged += MainWindow_StateChanged;
        }

        /// <summary>
        /// Wird beim Schließen des Fensters ausgelöst.
        /// - Minimiert das Fenster in den Tray, wenn gewünscht
        /// - Sonst: Standardverhalten (App beenden)
        /// </summary>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_minimizeOnClose)
            {
                // Schließen abfangen und stattdessen minimieren
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                if (_notifyIcon != null)
                    _notifyIcon.Visible = true;
            }
            // Sonst: Standardverhalten
        }

        /// <summary>
        /// Wird ausgelöst, wenn sich der Fensterstatus ändert (z.B. minimiert/wiederhergestellt).
        /// - Steuert Sichtbarkeit des Tray-Icons
        /// - Startet ggf. Animation im ViewModel
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                // Fenster bleibt in der Taskleiste sichtbar, Tray-Icon anzeigen
                this.ShowInTaskbar = true;
                if (_notifyIcon != null)
                    _notifyIcon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.ShowInTaskbar = true;
                if (_notifyIcon != null)
                    _notifyIcon.Visible = false;
            }

            // Animation im ViewModel starten, wenn Fenster normal
            if (this.WindowState == WindowState.Normal)
            {
                if (this.DataContext is ViewModels.MainWindowViewModel vm)
                    vm.PlayChartAnimationOnNextRefresh();
            }
        }

        /// <summary>
        /// Wird bei Doppelklick auf das Tray-Icon ausgelöst.
        /// - Stellt das Fenster wieder her und blendet das Tray-Icon aus
        /// </summary>
        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
            this.Activate();
        }

        /// <summary>
        /// Wird beim endgültigen Schließen des Fensters ausgelöst.
        /// - Tray-Icon wird entfernt und Ressourcen freigegeben
        /// </summary>
        /// <param name="e">Event-Argumente</param>
        protected override void OnClosed(EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// Klick auf den Schließen-Button in der UI.
        /// - Beendet die Anwendung sofort
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}