// NC_Setup_Assist/Helpers/FileHelper.cs
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace NC_Setup_Assist.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// Prüft, ob eine Datei existiert. Falls nicht, wird der Benutzer aufgefordert,
        /// die Datei manuell auszuwählen.
        /// </summary>
        /// <param name="originalPath">Der ursprüngliche Dateipfad aus der Datenbank</param>
        /// <param name="fileName">Der erwartete Dateiname (für die Fehlermeldung)</param>
        /// <returns>Der gültige Dateipfad oder null, wenn abgebrochen wurde</returns>
        public static string? GetValidFilePath(string originalPath, string? fileName = null)
        {
            // 1. Prüfen, ob die Datei am ursprünglichen Ort existiert
            if (File.Exists(originalPath))
            {
                return originalPath;
            }

            // 2. Benutzer informieren und um manuelle Auswahl bitten
            var result = MessageBox.Show(
                $"Die Datei konnte nicht gefunden werden:\n\n" +
                $"Ursprünglicher Pfad:\n{originalPath}\n\n" +
                $"Möchten Sie die Datei manuell auswählen?",
                "Datei nicht gefunden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return null;
            }

            // 3. Dateiauswahl-Dialog öffnen
            var openFileDialog = new OpenFileDialog
            {
                Filter = "NC-Programme (*.nc;*.NC)|*.nc;*.NC|Alle Dateien (*.*)|*.*",
                Title = $"Bitte wählen Sie die Datei aus: {fileName ?? Path.GetFileName(originalPath)}",
                FileName = fileName ?? Path.GetFileName(originalPath)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        /// <summary>
        /// Liest den Inhalt einer NC-Code-Datei.
        /// Falls die Datei nicht existiert, wird der Benutzer zur manuellen Auswahl aufgefordert.
        /// </summary>
        public static string LoadNcFileContent(string originalPath, string? fileName = null)
        {
            var validPath = GetValidFilePath(originalPath, fileName);

            if (validPath == null)
            {
                return $"Die Datei konnte nicht geladen werden.\n\n" +
                       $"Ursprünglicher Pfad:\n{originalPath}";
            }

            try
            {
                return File.ReadAllText(validPath);
            }
            catch (Exception ex)
            {
                return $"Fehler beim Lesen der Datei:\n{ex.Message}\n\n" +
                       $"Pfad:\n{validPath}";
            }
        }
    }
}