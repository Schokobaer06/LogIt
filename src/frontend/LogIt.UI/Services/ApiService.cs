using LogIt.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LogIt.UI.Services
{
    /// <summary>
    /// - Service für die Kommunikation mit dem Backend-API
    /// - Holt Log-Daten vom Server
    /// </summary>
    public class ApiService
    {
        /// <summary>
        /// - HTTP-Client für Anfragen an das Backend
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// - Konstruktor
        /// - Initialisiert HttpClient mit Basis-URL des Backends
        /// </summary>
        public ApiService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5000/api/")
            };
        }

        /// <summary>
        /// - Holt alle LogEntries (inkl. Sessions) vom Backend
        /// - Gibt eine Liste von LogEntry-Objekten zurück
        /// - Bei Fehler: Gibt leere Liste zurück
        /// </summary>
        /// <returns>Liste aller LogEntry-Objekte</returns>
        public async Task<List<LogEntry>> GetAllLogEntriesAsync()
        {
            try
            {
                return await _client
                    .GetFromJsonAsync<List<LogEntry>>("LogEntries/all")
                    ?? new List<LogEntry>();
            }
            catch
            {
                return new List<LogEntry>();
            }
        }
    }
}
