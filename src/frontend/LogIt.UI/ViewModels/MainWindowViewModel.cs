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

        public PlotModel UsagePlotModel { get; }

        public MainWindowViewModel()
        {
            _apiService = new ApiService();

            // Timer initialisieren
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (_, __) => await RefreshAsync();
            _timer.Start();

            // Chart-Setup
            UsagePlotModel = new PlotModel { Title = "Programm-Nutzung" };

            // Kategorie-Achse (Datum) entlang der Y-Achse, damit BarSeries horizontal stapelt
            var dateAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                Angle = 0,
                GapWidth = 0
            };
            UsagePlotModel.Axes.Add(dateAxis);

            // Wert-Achse (Stunden) entlang der X-Achse
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Title = "Stunden",
                MajorStep = 1,
                MinorStep = 0.25,
                StringFormat = "0.#" // Nur eine Nachkommastelle
            };
            UsagePlotModel.Axes.Add(valueAxis);

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

            // 2) Daten für den Chart aufbereiten
            //   Alle Sessions, gruppiert nach Tag
            var sessions = all.SelectMany(le => le.Sessions.Select(s => new { Session = s, le.ProgramName }))
                  .Where(x => x.Session.EndTime != null)
                  .ToList();

            var grouped = sessions
                .GroupBy(s => s.Session.StartTime.Date)
                .OrderBy(g => g.Key)
                .ToList();

            // 3) Programme ermitteln (einmalig)
            var programs = all.Select(s => s.ProgramName)
                                   .Distinct()
                                   .ToList();

            // 4) Serie pro Programm
            UsagePlotModel.Series.Clear();
            foreach (var prog in programs)
            {
                var series = new BarSeries
                {
                    Title = prog,
                    LabelPlacement = LabelPlacement.Outside,
                    LabelFormatString = "{0}", // Wir formatieren das Label gleich individuell
                    FillColor = OxyColor.Parse("#4F81BD"), // Beispiel-Farbe
                    StrokeColor = OxyColors.White,
                    StrokeThickness = 1.5
                };
                UsagePlotModel.Series.Add(series);
            }

            // 5) Achsenbeschriftung (Y-Achse)
            var catAxis = (CategoryAxis)UsagePlotModel.Axes.First(a => a is CategoryAxis);
            catAxis.Labels.Clear();
            foreach (var group in grouped)
                catAxis.Labels.Add(group.Key.ToString("dd.MM"));



            // 6) Daten in die Series füllen
            for (int dayIndex = 0; dayIndex < grouped.Count; dayIndex++)
            {
                var dayGroup = grouped[dayIndex];
                foreach (var series in UsagePlotModel.Series.OfType<BarSeries>())
                {
                    // Stunden für dieses Programm an diesem Tag
                    var hours = dayGroup
                        .Where(x => x.ProgramName == series.Title)
                        .Sum(x => x.Session.Duration.TotalHours);


                    var label = FormatDuration(hours);

                    series.Items.Add(new BarItem
                    {
                        Value = hours,
                        CategoryIndex = dayIndex
                    });
                    series.LabelFormatString = "{0:0.#}h";    // Damit das Label übernommen wird

                }
            }

            // 7) Refresh
            UsagePlotModel.InvalidatePlot(true);
        }

        private static string FormatDuration(double totalHours)
        {
            var ts = TimeSpan.FromHours(totalHours);
            if (ts.TotalHours < 0.01) return ""; // Kein Label bei sehr kleinen Werten
            if (ts.Hours > 0 && ts.Minutes > 0)
                return $"{ts.Hours}h {ts.Minutes}min";
            if (ts.Hours > 0)
                return $"{ts.Hours}h";
            if (ts.Minutes > 0)
                return $"{ts.Minutes}min";
            return "";
        }

    }
}
