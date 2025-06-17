using LogIt.Core.Data;
using LogIt.Core.Models;
using LogIt.Core.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

namespace LogIt.Core
{
    /// <summary>
    /// Main entry point for the LogIt.Core application.
    /// - Initialisiert Konfiguration, Logging, Datenbank und Webserver.
    /// - Startet den Webhost und stellt API-Endpunkte bereit.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry method.
        /// - Lädt Konfiguration aus appsettings.json.
        /// - Initialisiert Serilog für Logging.
        /// - Baut und konfiguriert den WebHost (API, CORS, Swagger, EF Core, Background Service).
        /// - Initialisiert und seedet die Datenbank mit Standard-Usern.
        /// - Startet den Webserver und behandelt Fehler beim Start.
        /// </summary>
        /// <param name="args">Kommandozeilenargumente für den Host.</param>
        public static void Main(string[] args)
        {
            /**
             * @brief Konfiguration aus appsettings.json laden.
             */
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            /**
             * @brief Serilog-Logger initialisieren (für Logging).
             */
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information("Starting host...");

                /**
                 * @brief WebApplication-Builder erstellen (Basis für Webserver).
                 */
                var builder = WebApplication.CreateBuilder(args);

                /**
                 * @brief Serilog als Logging-Provider verwenden.
                 */
                builder.Host.UseSerilog();

                /**
                 * @brief Entity Framework Core mit SQLite konfigurieren (Datenbankzugriff).
                 */
                builder.Services.AddDbContext<LogItDbContext>(opt =>
                    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

                /**
                 * @brief CORS-Policy: Erlaubt alle Ursprünge, Header und Methoden (für API-Zugriff von überall).
                 */
                builder.Services.AddCors(opt =>
                {
                    opt.AddPolicy("AllowAll", p =>
                        p.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod());
                });

                /**
                 * @brief Controller und JSON-Optionen konfigurieren.
                 */
                builder.Services
                    .AddControllers()
                    .AddJsonOptions(x =>
                    {
                        /**
                         * @brief Verhindert Referenzzyklen beim Serialisieren (z.B. bei verschachtelten Objekten).
                         */
                        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                        // Optional: Ignoriert Nullwerte beim Serialisieren
                        // x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    });

                /**
                 * @brief Hintergrunddienst für Prozessüberwachung hinzufügen.
                 */
                builder.Services.AddHostedService<ProcessMonitorService>();

                /**
                 * @brief API-Dokumentation (Swagger) aktivieren.
                 */
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                /**
                 * @brief Anwendung bauen (Webserver-Instanz erzeugen).
                 */
                var app = builder.Build();

                /// <summary>
                /// Initialisiert die Datenbank:
                /// - Erstellt das Schema, falls nicht vorhanden.
                /// - Fügt Standard-User (System, Backend, Frontend) hinzu, falls sie fehlen.
                /// </summary>
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();

                    /**
                     * @brief Stellt sicher, dass das Datenbankschema existiert.
                     */
                    db.Database.EnsureCreated();
                    Log.Information("Database structure ensured via EnsureCreated().");

                    /**
                     * @brief Standard-User anlegen, falls nicht vorhanden.
                     */
                    if (!db.Users.Any(u => u.Role == UserRole.System))
                    {
                        db.Users.Add(new User { Role = UserRole.System });
                    }
                    if (!db.Users.Any(u => u.Role == UserRole.Backend))
                    {
                        db.Users.Add(new User { Role = UserRole.Backend });
                    }
                    if (!db.Users.Any(u => u.Role == UserRole.Frontend))
                    {
                        db.Users.Add(new User { Role = UserRole.Frontend });
                    }
                    db.SaveChanges();
                    Log.Information("Default users ensured: System, Backend, Frontend.");
                }

                /**
                 * @brief Loggt automatisch alle HTTP-Anfragen.
                 */
                app.UseSerilogRequestLogging();

                /**
                 * @brief CORS-Policy anwenden (API von überall erreichbar).
                 */
                app.UseCors("AllowAll");

                /**
                 * @brief Swagger für API-Dokumentation aktivieren.
                 */
                app.UseSwagger();
                app.UseSwaggerUI();

                /**
                 * @brief Controller-Endpunkte registrieren (API-Routen aktivieren).
                 */
                app.MapControllers();

                Log.Information("Host is running");
                app.Run();
            }
            catch (Exception ex)
            {
                /**
                 * @brief Fehler beim Start werden geloggt.
                 */
                Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                /**
                 * @brief Logger sauber schließen (Ressourcen freigeben).
                 */
                Log.CloseAndFlush();
            }
        }
    }
}
