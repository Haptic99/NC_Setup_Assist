// NC_Setup_Assist/ViewModels/ToolAssignmentComparisonViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services; // <-- NEU
using System; // <-- NEU
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; // <-- NEU

namespace NC_Setup_Assist.ViewModels
{
    // ... (ToolComparisonItem ViewModel bleibt unverändert) ...
    public partial class ToolComparisonItem : ObservableObject
    {
        public string Station { get; }
        public string Korrektur { get; }
        // public string ToolNameBefore { get; } // Entfernt
        public string ToolNameAfter { get; }
        public string AssignmentStatus { get; }
        public string? FräserAusrichtung { get; } // Umbenannt
        public string ToolSubCategory { get; }

        // --- NEUE FELDER ---
        public string? FavoritKategorie { get; }
        public string? FavoritUnterkategorie { get; }
        // -----------------

        // Indikator für die Zuweisung: True, wenn manuell zugewiesen ODER als Standardwerkzeug erkannt.
        public bool IsAssigned => AssignmentStatus != "Unassigned";

        // Konstruktor angepasst
        public ToolComparisonItem(string station,
                                  string korrektur,
                                  // string before, // Entfernt
                                  string after,
                                  string status,
                                  string? fräserAusrichtung, // Umbenannt
                                  string toolSubCategory,
                                  string? favoritKategorie, // Neu
                                  string? favoritUnterkategorie) // Neu
        {
            Station = station;
            Korrektur = korrektur;
            // ToolNameBefore = before; // Entfernt
            ToolNameAfter = after;
            AssignmentStatus = status;
            FräserAusrichtung = fräserAusrichtung; // Umbenannt
            ToolSubCategory = toolSubCategory;
            FavoritKategorie = favoritKategorie; // Neu
            FavoritUnterkategorie = favoritUnterkategorie; // Neu
        }
    }


    public partial class ToolAssignmentComparisonViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly NCProgramm _programm;

        public ObservableCollection<ToolComparisonItem> ComparisonItems { get; } = new();

        public ToolAssignmentComparisonViewModel(
            MainViewModel mainViewModel,
            NCProgramm programm,
            List<WerkzeugEinsatz> finalAssignments) // Parameter wird nicht mehr verwendet, da Daten neu geladen werden.
        {
            _mainViewModel = mainViewModel;
            _programm = programm;

            // Wir laden die Daten beim Start
            LoadComparisonData();
            // NEU: Initialer Check des Command-Status für den "Fertig"-Button
            FinishAssignmentCommand.NotifyCanExecuteChanged();
        }

        private void LoadComparisonData()
        {
            // --- NEU: try-catch ---
            try
            {
                ComparisonItems.Clear();

                using var context = new NcSetupContext();

                // 1. Lade Standardwerkzeuge der Maschine (für "Before"-Status und als Fallback für "After"-Status)
                // ANGEPASST: .Kategorie mitladen
                var standardTools = context.StandardWerkzeugZuweisungen
                                            .Where(z => z.MaschineID == _programm.MaschineID)
                                            .Include(z => z.ZugehoerigesWerkzeug)
                                                .ThenInclude(w => w!.Unterkategorie.Kategorie) // Wichtig: Unterkategorie UND Kategorie mitladen
                                            .ToList();

                var standardToolLookup = standardTools.ToDictionary(
                    z => z.RevolverStation.ToString(),
                    z => z.ZugehoerigesWerkzeug
                );

                // 2. Lade alle eindeutigen Werkzeugeinsätze des Programms, diesmal DIREKT aus der DB,
                // um den aktuellen Zuweisungsstatus zu sehen.
                // ANGEPASST: .Kategorie mitladen
                var allUniqueToolsEntities = context.WerkzeugEinsaetze
                    .Where(e => e.NCProgrammID == _programm.NCProgrammID && !string.IsNullOrEmpty(e.RevolverStation))
                    .Include(e => e.ZugehoerigesWerkzeug)
                        .ThenInclude(w => w!.Unterkategorie.Kategorie) // Lade das zugewiesene Werkzeug, dessen Unterkategorie UND Kategorie
                    .AsEnumerable() // Wechsle zu In-Memory-Verarbeitung
                    .GroupBy(e => new { e.RevolverStation, e.KorrekturNummer })
                    .Select(g => g.First())
                    .ToList();


                // 3. Verarbeite die Vergleichsdaten
                foreach (var uniqueTool in allUniqueToolsEntities.OrderBy(t => t.Reihenfolge).ThenBy(t => t.RevolverStation).ThenBy(t => t.KorrekturNummer))
                {
                    // ... (Rest der foreach-Logik bleibt unverändert)
                    string stationKey = uniqueTool.RevolverStation!.TrimStart('0');
                    string korrekturKey = uniqueTool.KorrekturNummer ?? "";
                    Werkzeug? stdTool;
                    standardToolLookup.TryGetValue(stationKey, out stdTool);
                    string toolAfterName;
                    string assignmentStatus;
                    string toolSubCategory;

                    if (uniqueTool.ZugehoerigesWerkzeug != null)
                    {
                        toolAfterName = uniqueTool.ZugehoerigesWerkzeug.Name;
                        toolSubCategory = uniqueTool.ZugehoerigesWerkzeug.Unterkategorie?.Name ?? "k.A.";

                        if (uniqueTool.Kommentar?.StartsWith("Favorit") == true)
                        {
                            assignmentStatus = "Favorite";
                        }
                        else if (stdTool != null && stdTool.WerkzeugID == uniqueTool.ZugehoerigesWerkzeug.WerkzeugID)
                        {
                            assignmentStatus = "Standard";
                        }
                        else
                        {
                            assignmentStatus = "Manual";
                        }
                    }
                    else
                    {
                        if (stdTool != null)
                        {
                            toolAfterName = $"{stdTool.Name}";
                            toolSubCategory = stdTool.Unterkategorie?.Name ?? "k.A.";
                            assignmentStatus = "Standard";
                        }
                        else
                        {
                            toolAfterName = "Unzugewiesen";
                            assignmentStatus = "Unassigned";
                            if (!string.IsNullOrEmpty(uniqueTool.FavoritUnterkategorie))
                            {
                                toolSubCategory = $"Vorschlag: {uniqueTool.FavoritUnterkategorie}";
                            }
                            else if (!string.IsNullOrEmpty(uniqueTool.FavoritKategorie))
                            {
                                toolSubCategory = $"Vorschlag: {uniqueTool.FavoritKategorie}";
                            }
                            else
                            {
                                toolSubCategory = "---";
                            }
                        }
                    }

                    ComparisonItems.Add(new ToolComparisonItem(
                        uniqueTool.RevolverStation!,
                        korrekturKey,
                        toolAfterName,
                        assignmentStatus,
                        uniqueTool.FräserAusrichtung, // Umbenannt
                        toolSubCategory,
                        uniqueTool.FavoritKategorie, // Neu
                        uniqueTool.FavoritUnterkategorie // Neu
                    ));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Vergleichsdaten");
                MessageBox.Show($"Fehler beim Laden der Vergleichsdaten:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Befehl für den Doppelklick (zum manuellen Zuweisen)
        [RelayCommand]
        private void AssignTool(ToolComparisonItem? item)
        {
            if (item == null) return;

            string station = item.Station;
            string korrektur = item.Korrektur;

            // --- NEU: Favoriten aus dem Item holen ---
            string? favoritKat = item.FavoritKategorie;
            string? favoritUnterKat = item.FavoritUnterkategorie;
            // ----------------------------------------

            // Navigation zum ToolManagementViewModel (Auswahlmodus)
            // --- KORREKTUR: MainViewModel als erstes Argument, Favoriten übergeben ---
            var toolManagementVM = new ToolManagementViewModel(
                _mainViewModel,
                selectedTool => // Callback
                {
                    PerformToolAssignmentUpdate(station, korrektur, selectedTool.WerkzeugID);
                    _mainViewModel.NavigateBack();
                },
                favoritKat, // NEU
                favoritUnterKat // NEU
            );

            _mainViewModel.NavigateTo(toolManagementVM);
        }

        // Logik zum Speichern der Zuweisung in der Datenbank
        private void PerformToolAssignmentUpdate(string station, string korrektur, int werkzeugId)
        {
            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();

                // Finde ALLE WerkzeugEinsatz-Objekte mit der gleichen RevolverStation und KorrekturNummer im aktuellen NC-Programm
                var einsaetzeToUpdate = context.WerkzeugEinsaetze
                    .Where(e => e.NCProgrammID == _programm.NCProgrammID &&
                                e.RevolverStation == station &&
                                (e.KorrekturNummer == korrektur || (string.IsNullOrEmpty(korrektur) && e.KorrekturNummer == null)))
                    .ToList();

                // Aktualisiere die WerkzeugID in der Datenbank
                foreach (var einsatz in einsaetzeToUpdate)
                {
                    einsatz.WerkzeugID = werkzeugId;
                    // WICHTIG: Entferne die "Favorit"-Markierung, da es jetzt eine manuelle Zuweisung ist
                    if (einsatz.Kommentar?.StartsWith("Favorit") == true)
                    {
                        einsatz.Kommentar = null;
                    }
                    // Favoriten-Vorschläge auch leeren, da Zuweisung erfolgt ist
                    einsatz.FavoritKategorie = null;
                    einsatz.FavoritUnterkategorie = null;
                }
                context.SaveChanges();

                // Lade die Daten in der Vergleichsansicht neu, um die UI zu aktualisieren
                LoadComparisonData();

                // NEU: Benachrichtigt den Fertig-Button über den geänderten Status
                FinishAssignmentCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Speichern der Werkzeugzuweisung");
                MessageBox.Show($"Fehler beim Speichern der Zuweisung:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NEU: Command zum Abschließen der Zuweisung und Zurückkehren
        [RelayCommand(CanExecute = nameof(CanFinishAssignment))]
        private void FinishAssignment()
        {
            // Alle Zuweisungen sind bereits in PerformToolAssignmentUpdate gespeichert.
            // Wir navigieren einfach zurück zum Analyse-ViewModel.
            _mainViewModel.NavigateBack();
        }

        // NEU: Logik, die bestimmt, ob der Fertig-Button klickbar ist
        private bool CanFinishAssignment()
        {
            // Das Projekt ist "fertig", wenn alle Einträge eine Zuweisung haben.
            return ComparisonItems.Any() && ComparisonItems.All(item => item.IsAssigned);
        }
    }
}