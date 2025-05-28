
using LogIt.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LogIt.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // **Datenbank-Initialisierung**
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();
                try
                {
                    // Versuche, alle Migrationen anzuwenden (erstellt fehlende Tabellen)
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // Bei Fehlern (z. B. inkonsistente Struktur) löschen und neu erstellen
                    Console.WriteLine($"Datenbank-Migration fehlgeschlagen: {ex.Message}\nErstelle DB neu...");
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                }
            }

            app.UseCors("AllowAll");
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapControllers();
            app.Run();
        }
    }
}
