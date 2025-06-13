using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LogIt.Core.Models;
using LogIt.UI.Services;
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

        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        public string AppVersion => $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        // Animation nur beim ersten Öffnen des MainWindows
        private bool _isFirstLoad = true;
        public void PlayChartAnimationOnNextRefresh()
        {
            _isFirstLoad = true;
        }

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

            // 2) Sessions pro Tag und Programm aggregieren
            var now = DateTime.Now;
            var sessions = all
                .SelectMany(le => le.Sessions.Select(s => new
                {
                    ProgramName = le.ProgramName,
                    Day = s.StartTime.Date,
                    Duration = (s.EndTime ?? now) - s.StartTime
                }))
                .Where(x => x.Duration.TotalSeconds > 0)
                .ToList();

            // Alle Tage, an denen geloggt wurde (sortiert)
            var allDays = sessions
                .Select(x => x.Day)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // Alle Programme, die jemals geloggt wurden
            var allPrograms = sessions
                .Select(x => x.ProgramName)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            // 3) X-Achsen-Beschriftung: alle 7 Tage ein Label, sonst leer
            Labels = allDays.Select((d, i) =>
                i % 7 == 0 ? d.ToString("dd. MMM") : "").ToArray();
            RaisePropertyChanged(nameof(Labels));

            // 4) Matrix: Für jedes Programm, für jeden Tag die Nutzungszeit (in Stunden)
            var values = new double[allPrograms.Count][];
            for (int i = 0; i < allPrograms.Count; i++)
            {
                var prog = allPrograms[i];
                values[i] = allDays
                    .Select(day => sessions
                        .Where(x => x.ProgramName == prog && x.Day == day)
                        .Sum(x => x.Duration.TotalHours))
                    .ToArray();
            }

            // 5) Farben für die Programme
            var colorPalette = new[]
            {
                SkiaSharp.SKColors.SteelBlue, SkiaSharp.SKColors.Orange, SkiaSharp.SKColors.MediumSeaGreen,
                SkiaSharp.SKColors.MediumVioletRed, SkiaSharp.SKColors.Goldenrod, SkiaSharp.SKColors.MediumSlateBlue,
                SkiaSharp.SKColors.Crimson, SkiaSharp.SKColors.Teal, SkiaSharp.SKColors.DarkCyan,
                SkiaSharp.SKColors.DarkOrange, SkiaSharp.SKColors.DarkMagenta, SkiaSharp.SKColors.DarkGreen
            };

            // 6) Series für den Chart
            Series = allPrograms
                .Select((prog, i) =>
                    (ISeries)new StackedColumnSeries<double>
                    {
                        Name = prog,
                        Values = values[i],
                        XToolTipLabelFormatter = point =>
                        {
                            int idx = point.Index;
                            if (idx >= 0 && idx < allDays.Count)
                                return allDays[idx].ToString("dd. MMM yyyy");
                            return "";
                        },
                        YToolTipLabelFormatter = point =>
                        {
                            var hours = point.Coordinate.PrimaryValue;
                            var ts = TimeSpan.FromHours(hours);
                            if (ts.TotalSeconds < 1) return $"{prog}: 0s";
                            if (ts.TotalHours >= 1)
                                return $"{prog}: {ts.Hours}h {ts.Minutes}min";
                            if (ts.TotalMinutes >= 1)
                                return $"{prog}: {ts.Minutes}min {ts.Seconds}s";
                            return $"{prog}: {ts.Seconds}s";
                        },
                        Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(colorPalette[i % colorPalette.Length]),
                        Stroke = null
                    })
                .ToArray();

            RaisePropertyChanged(nameof(Series));

            // 7) Achsen setzen
            XAxes = new[]
            {
                new Axis
                {
                    Labels = Labels,
                    LabelsRotation = 0,
                    MinStep = 1,
                    Name = "Tag",
                    TextSize = 14,
                    Padding = new LiveChartsCore.Drawing.Padding(10)
                }
            };
            YAxes = new[]
            {
                new Axis
                {
                    Name = "Stunden",
                    TextSize = 14,
                    MinLimit = 0,
                    Labeler = YFormatter
                }
            };
            RaisePropertyChanged(nameof(XAxes));
            RaisePropertyChanged(nameof(YAxes));

            // Animation nur beim ersten Öffnen
            foreach (var s in Series.OfType<StackedColumnSeries<double>>())
                s.AnimationsSpeed = _isFirstLoad ? TimeSpan.FromMilliseconds(400) : TimeSpan.Zero;

            _isFirstLoad = false;
        }
    }
}
