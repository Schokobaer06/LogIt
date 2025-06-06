using LogIt.Core.Models;
using LogIt.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LogIt.UI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Die gebundene Liste für das DataGrid
        /// </summary>
        public ObservableCollection<LogEntryDisplay> Entries { get; }
            = new ObservableCollection<LogEntryDisplay>();

        public MainWindowViewModel()
        {
            _apiService = new ApiService();

            // Timer: alle 1 Sekunde neu laden
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += async (_, __) => await RefreshAsync();
            _timer.Start();
        }

        /// <summary>
        /// Holt alle Einträge, mapped auf LogEntryDisplay, sortiert und aktualisiert Entries
        /// </summary>
        public async Task RefreshAsync()
        {
            var all = await _apiService.GetAllLogEntriesAsync();

            var displays = all
                .Select(le => new LogEntryDisplay(le))
                // Aktive zuerst (true > false), dann SortKey absteigend
                .OrderByDescending(d => d.IsActive)
                .ThenByDescending(d => d.SortKey)
                .ToList();

            Entries.Clear();
            foreach (var d in displays)
                Entries.Add(d);
        }
    }
}
