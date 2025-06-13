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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainWindowViewModel();
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/app.ico")); // Taskleisten-Icon

            // Create NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("app.ico"), // You can use your own .ico file here
                Visible = false,
                Text = "LogIt"
            };

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
                    // Remove event handler to allow closing
                    this.Closing -= MainWindow_Closing;
                    this.Close();
                });
            });
            _notifyIcon.ContextMenuStrip = menu;

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            var arg = Environment.GetCommandLineArgs();
            if (arg.Contains("--minimized"))
            {
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                _notifyIcon.Visible = true;
            }

            this.Closing += MainWindow_Closing;
            this.StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Intercept and minimize instead
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            if (_notifyIcon != null)
                _notifyIcon.Visible = true;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                // Fenster bleibt in der Taskleiste sichtbar
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

            if (this.WindowState == WindowState.Normal)
            {
                if (this.DataContext is ViewModels.MainWindowViewModel vm)
                    vm.PlayChartAnimationOnNextRefresh();
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
            this.Activate();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }
    }
}