
using LogIt.Core.Data;
using LogIt.Core.Models;
using LogIt.Core.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

namespace LogIt.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();


            try
            {
                Log.Information("Starting host...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                // EF Core + SQLite
                builder.Services.AddDbContext<LogItDbContext>(opt =>
                    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

                builder.Services.AddCors(opt =>
                {
                    opt.AddPolicy("AllowAll", p =>
                        p.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod());
                });

                builder.Services
                    .AddControllers()
                    .AddJsonOptions(x =>
                    {
                        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                        // Optional: x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    });
                builder.Services.AddHostedService<ProcessMonitorService>();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();

                // **Datenbank-Initialisierung**
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();

                    // 1) Stelle sicher, dass das Schema wirklich vorhanden ist (Erzeuge alle Tabellen)
                    db.Database.EnsureCreated();
                    Log.Information("Database structure ensured via EnsureCreated().");

                    // 2) Seed default Users
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



                app.UseSerilogRequestLogging(); // Protokolliert jede HTTP-Anfrage automatisch
                app.UseCors("AllowAll");
                app.UseSwagger();
                app.UseSwaggerUI();
                app.MapControllers();

                Log.Information("Host is running");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
