using System;
using System.IO;
using System.Text;

namespace NC_Setup_Assist.Services
{
    public static class LoggingService
    {
        // --- HILFSMETHODE FÜR DEN PFAD ---
        private static string GetAppDataPath(string fileName)
        {
            string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificDir = Path.Combine(appDataDir, "NC_Setup_Assist");

            // Sicherstellen, dass das Verzeichnis existiert
            if (!Directory.Exists(appSpecificDir))
            {
                Directory.CreateDirectory(appSpecificDir);
            }

            return Path.Combine(appSpecificDir, fileName);
        }
        // ----------------------------------

        // --- ÄNDERUNG HIER ---
        // Der Pfad ist derselbe wie der deiner Datenbank
        // private static readonly string _logFilePath = Path.Combine(
        //     AppDomain.CurrentDomain.BaseDirectory,
        //     "error_log.txt"
        // ); // ALT

        private static readonly string _logFilePath = GetAppDataPath("error_log.txt"); // NEU

        // Öffentliche Eigenschaft, damit die App.xaml.cs den Pfad anzeigen kann
        public static string LogFilePath => _logFilePath;
        // --- ENDE ÄNDERUNG ---


        // Eine Sperre, um zu verhindern, dass zwei Fehler gleichzeitig in die Datei schreiben
        private static readonly object _lock = new object();

        public static void LogException(Exception ex, string contextMessage = "Error")
        {
            try
            {
                lock (_lock)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine($"Zeitstempel: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                    sb.AppendLine($"Kontext:     {contextMessage}");
                    sb.AppendLine($"Fehlertyp:   {ex.GetType().Name}");
                    sb.AppendLine($"Nachricht:   {ex.Message}");
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine(ex.StackTrace);
                    sb.AppendLine("==================================================\n");

                    // Hängt den Text an die Datei an. Wenn die Datei nicht existiert, wird sie erstellt.
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
            }
            catch (Exception)
            {
                // Wenn selbst das Logging fehlschlägt... können wir nichts tun.
            }
        }
    }
}