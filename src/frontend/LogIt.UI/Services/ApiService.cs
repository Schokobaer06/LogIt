using LogIt.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LogIt.UI.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5000/api/")
                // Passe hier Port/URL an, falls dein Backend unter anderem Port läuft.
            };
        }

        /// <summary>
        /// Holt alle LogEntries (inkl. Sessions) vom Backend
        /// </summary>
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
                // Im Fehlerfall geben wir eine leere Liste zurück
                return new List<LogEntry>();
            }
        }
    }
}
