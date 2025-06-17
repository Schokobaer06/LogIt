using LiveChartsCore;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LogIt.Core.Models;
using LogIt.UI.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LogIt.UI.ViewModels
{
    /// <summary>
    /// ViewModel für das Hauptfenster.
    /// - Stellt Daten für die UI bereit (Tabellen, Diagramme, Labels)
    /// - Holt Daten vom Backend
    /// - Bereitet Daten für LiveCharts2 auf
    /// - Aktualisiert regelmäßig die Anzeige
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// Service für API-Zugriffe (Backend-Kommunikation)
        /// </summary>
        private readonly ApiService _apiService;

        /// <summary>
        /// Timer für regelmäßige Aktualisierung der Daten
        /// </summary>
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Liste der anzuzeigenden Log-Einträge (für die Tabelle)
        /// </summary>
        public ObservableCollection<LogEntryDisplay> Entries { get; }
            = new ObservableCollection<LogEntryDisplay>();

        /// <summary>
        /// Datenreihen für das Diagramm (LiveCharts2)
        /// </summary>
        public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();

        /// <summary>
        /// Beschriftungen für die X-Achse im Diagramm
        /// </summary>
        public string[] Labels { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Formatierungsfunktion für Y-Achse (z.B. "2.5h")
        /// </summary>
        public Func<double, string> YFormatter { get; }
            = value => $"{value:0.#}h";

        /// <summary>
        /// X-Achsen-Objekte für das Diagramm
        /// </summary>
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();

        /// <summary>
        /// Y-Achsen-Objekte für das Diagramm
        /// </summary>
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        /// <summary>
        /// Versionsnummer der Anwendung als String
        /// </summary>
        public string AppVersion => $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        /// <summary>
        /// Paint-Objekt für Achsen/Diagramm (Farbe: LightGray)
        /// </summary>
        public Paint paint { get;} = new SolidColorPaint(SkiaSharp.SKColors.LightGray);

        /// <summary>
        /// Gibt an, ob die Chart-Animation beim nächsten Refresh abgespielt werden soll
        /// </summary>
        private bool _isFirstLoad = true;

        /// <summary>
        /// Setzt die Animation für das Diagramm beim nächsten Refresh zurück
        /// </summary>
        public void PlayChartAnimationOnNextRefresh()
        {
            _isFirstLoad = true;
        }

        /// <summary>
        /// Konstruktor.
        /// - Initialisiert API-Service und Timer
        /// - Startet regelmäßige Aktualisierung
        /// </summary>
        public MainWindowViewModel()
        {
            _apiService = new ApiService();

            // Timer initialisieren (jede Sekunde)
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (_, __) => await RefreshAsync();
            _timer.Start();

            // Erstes Laden der Daten
            _ = RefreshAsync();
        }

        /// <summary>
        /// Holt aktuelle Daten vom Backend und bereitet sie für die Anzeige und das Diagramm auf.
        /// - Aktualisiert Tabelle und Diagramm
        /// - Berechnet Nutzungszeiten pro Tag und Programm
        /// - Setzt Achsen und Labels für das Diagramm
        /// </summary>
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

            // 2) Sessions pro Tag und Programm aufteilen
            var sessions = new List<(string ProgramName, DateTime Day, TimeSpan Duration)>();
            foreach (var le in all)
            {
                foreach (var s in le.Sessions)
                {
                    var start = s.StartTime;
                    var end = s.EndTime ?? DateTime.Now;
                    var dayStart = start.Date;
                    var dayEnd = end.Date;

                    for (var day = dayStart; day <= dayEnd; day = day.AddDays(1))
                    {
                        // Anfang und Ende für diesen Tag
                        var rangeStart = day == dayStart ? start : day;
                        var rangeEnd = day == dayEnd ? end : day.AddDays(1);

                        var duration = rangeEnd - rangeStart;
                        if (duration.TotalSeconds > 0)
                        {
                            sessions.Add((le.ProgramName, day, duration));
                        }
                    }
                }
            }

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
                    Padding = new LiveChartsCore.Drawing.Padding(10),
                    NamePaint = new SolidColorPaint(SKColors.White), // Überschrift
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray) // Achsenbeschriftung
                }
            };
            YAxes = new[]
            {
                new Axis
                {
                    Name = "Summe Stunden der Programme",
                    TextSize = 14,
                    MinLimit = 0,
                    Labeler = YFormatter,
                    NamePaint = new SolidColorPaint(SKColors.White),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray)
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
