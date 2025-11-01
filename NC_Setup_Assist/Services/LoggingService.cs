using System;
using System.IO;
using System.Text;

namespace NC_Setup_Assist.Services
{
    public static class LoggingService
    {
        // Der Pfad ist derselbe wie der deiner Datenbank
        private static readonly string _logFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "error_log.txt"
        );

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