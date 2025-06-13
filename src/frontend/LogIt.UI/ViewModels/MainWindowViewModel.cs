using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LogIt.Core.Models;
using LogIt.UI.Services;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
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

        public ObservableCollection<LogEntryDisplay> Entries { get; }
            = new ObservableCollection<LogEntryDisplay>();

        // Für LiveCharts2:
        public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();
        public string[] Labels { get; private set; } = Array.Empty<string>();
        public Func<double, string> YFormatter { get; }
            = value => $"{value:0.#}h";

        public MainWindowViewModel()
        {
            _apiService = new ApiService();

            // Timer initialisieren
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (_, __) => await RefreshAsync();
            _timer.Start();

            

            // Erstes Laden
            _ = RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            // 1) Tabelle aktualisieren
            var all = await _apiService.GetAllLogEntriesAsync();
            var displayList = all
                .Select(le => new LogEntryDisplay(le))
                .OrderByDescending(d => d.IsActive)
                .ThenByDescending(d => d.SortKey)
                .ToList();

            Entries.Clear();
            foreach (var d in displayList)
                Entries.Add(d);

            // 2. Chart-Daten aufbereiten
            // a) alle Sessions pro Tag+Programm
            var sessionData = all
                .SelectMany(le => le.Sessions, (le, s) => new {
                    Day = s.StartTime.Date,
                    Program = le.ProgramName,
                    Hours = (s.EndTime ?? DateTime.Now) - s.StartTime
                })
                .Where(x => x.Hours.TotalSeconds > 0)
                .GroupBy(x => new { x.Day, x.Program })
                .Select(g => new {
                    Day = g.Key.Day,
                    Program = g.Key.Program,
                    Hours = g.Sum(x => x.Hours.TotalHours)
                })
                .ToList();

            // b) eindeutige Tage und Programme
            var days = sessionData.Select(x => x.Day).Distinct().OrderBy(d => d).ToArray();
            var programs = sessionData.Select(x => x.Program).Distinct().ToArray();

            // c) Labels setzen (X-Achse)
            Labels = days.Select(d => d.ToString("dd.MM")).ToArray();
            RaisePropertyChanged(nameof(Labels));

            // d) Matrix [programm][tagIndex]
            var values = new double[programs.Length][];
            for (int i = 0; i < programs.Length; i++)
            {
                var prog = programs[i];
                values[i] = days
                    .Select(day => sessionData
                        .Where(x => x.Program == prog && x.Day == day)
                        .Sum(x => x.Hours))
                    .ToArray();
            }

            // e) eine StackedColumnSeries pro Programm
            Series = programs
                .Select((prog, i) =>
                    (ISeries)new StackedColumnSeries<double>
                    {
                        Name = prog,
                        Values = values[i],
                        TooltipLabelFormatter = point => $"{prog}: {point.PrimaryValue:0.#}h"
                    })
                .ToArray();
            RaisePropertyChanged(nameof(Series));
        }
    }
}
