// NC_Setup_Assist/ViewModels/ToolAssignmentComparisonViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NC_Setup_Assist.ViewModels
{
    // Hilfs-ViewModel für eine einzelne Vergleichszeile
    public partial class ToolComparisonItem : ObservableObject
    {
        public string Station { get; }
        public string Korrektur { get; }
        public string ToolNameBefore { get; }
        public string ToolNameAfter { get; }
        public string AssignmentStatus { get; } // NEU
        public string? BearbeitungsArt { get; } // NEU (für Fräsen-Filter)
        public string ToolSubCategory { get; } // NEU: Eigenschaft für die Unterkategorie

        // Indikator für die Zuweisung: True, wenn manuell zugewiesen ODER als Standardwerkzeug erkannt.
        public bool IsAssigned => AssignmentStatus != "Unassigned"; // Angepasst

        // Konstruktor angepasst
        public ToolComparisonItem(string station, string korrektur, string before, string after, string status, string? bearbeitungsArt, string toolSubCategory) // Angepasst
        {
            Station = station;
            Korrektur = korrektur;
            ToolNameBefore = before;
            ToolNameAfter = after;
            AssignmentStatus = status; // NEU
            BearbeitungsArt = bearbeitungsArt; // NEU
            ToolSubCategory = toolSubCategory; // NEU
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
            ComparisonItems.Clear();

            using var context = new NcSetupContext();

            // 1. Lade Standardwerkzeuge der Maschine (für "Before"-Status und als Fallback für "After"-Status)
            var standardTools = context.StandardWerkzeugZuweisungen
                                        .Where(z => z.MaschineID == _programm.MaschineID)
                                        .Include(z => z.ZugehoerigesWerkzeug)
                                            .ThenInclude(w => w!.Unterkategorie) // Wichtig: Unterkategorie mitladen
                                        .ToList();

            var standardToolLookup = standardTools.ToDictionary(
                z => z.RevolverStation.ToString(),
                z => z.ZugehoerigesWerkzeug
            );

            // 2. Lade alle eindeutigen Werkzeugeinsätze des Programms, diesmal DIREKT aus der DB,
            // um den aktuellen Zuweisungsstatus zu sehen.
            // ÄNDERUNG: .Include für ZugehoerigesWerkzeug.Unterkategorie hinzugefügt
            var allUniqueToolsEntities = context.WerkzeugEinsaetze
                .Where(e => e.NCProgrammID == _programm.NCProgrammID && !string.IsNullOrEmpty(e.RevolverStation))
                .Include(e => e.ZugehoerigesWerkzeug)
                    .ThenInclude(w => w!.Unterkategorie) // Lade das zugewiesene Werkzeug und dessen Unterkategorie
                .AsEnumerable() // Wechsle zu In-Memory-Verarbeitung
                .GroupBy(e => new { e.RevolverStation, e.KorrekturNummer })
                .Select(g => g.First())
                .ToList();


            // 3. Verarbeite die Vergleichsdaten
            foreach (var uniqueTool in allUniqueToolsEntities.OrderBy(t => t.RevolverStation).ThenBy(t => t.KorrekturNummer))
            {
                string stationKey = uniqueTool.RevolverStation!.TrimStart('0');
                string korrekturKey = uniqueTool.KorrekturNummer ?? "";

                // --- Bestimme "Before" Status (Standardwerkzeug) ---
                string toolBeforeName;
                Werkzeug? stdTool;
                if (standardToolLookup.TryGetValue(stationKey, out stdTool))
                {
                    toolBeforeName = $"{stdTool!.Name}";
                }
                else
                {
                    toolBeforeName = "Unbekannt";
                }

                // --- Bestimme "After" Status (Final zugewiesen) ---
                string toolAfterName;
                string assignmentStatus; // NEU
                string toolSubCategory; // NEU

                if (uniqueTool.ZugehoerigesWerkzeug != null)
                {
                    toolAfterName = uniqueTool.ZugehoerigesWerkzeug.Name;
                    // NEU: Unterkategorie-Name holen
                    toolSubCategory = uniqueTool.ZugehoerigesWerkzeug.Unterkategorie?.Name ?? "k.A.";

                    // NEU: Status bestimmen
                    if (uniqueTool.Kommentar?.StartsWith("Favorit") == true)
                    {
                        assignmentStatus = "Favorite"; // Blau
                    }
                    else if (stdTool != null && stdTool.WerkzeugID == uniqueTool.ZugehoerigesWerkzeug.WerkzeugID)
                    {
                        // Manuell zugewiesen, aber es ist dasselbe wie das Standardwerkzeug
                        assignmentStatus = "Standard"; // Grün
                    }
                    else
                    {
                        // Manuell zugewiesen, und es ist NICHT das Standardwerkzeug
                        assignmentStatus = "Manual"; // Gelb/Rot
                    }
                }
                else
                {
                    // Keine manuelle/parser-Zuweisung
                    if (stdTool != null)
                    {
                        // Standardwerkzeug gefunden
                        toolAfterName = $"{stdTool.Name}";
                        // NEU: Unterkategorie-Name holen
                        toolSubCategory = stdTool.Unterkategorie?.Name ?? "k.A.";
                        assignmentStatus = "Standard"; // Grün
                    }
                    else
                    {
                        // Weder manuell zugewiesen noch Standard
                        toolAfterName = "Unzugewiesen";
                        toolSubCategory = "---"; // NEU
                        assignmentStatus = "Unassigned"; // Gelb
                    }
                }

                ComparisonItems.Add(new ToolComparisonItem(
                    uniqueTool.RevolverStation!,
                    korrekturKey,
                    toolBeforeName,
                    toolAfterName,
                    assignmentStatus, // NEU
                    uniqueTool.BearbeitungsArt, // NEU
                    toolSubCategory // NEU
                ));
            }
        }

        // Befehl für den Doppelklick (zum manuellen Zuweisen)
        [RelayCommand]
        private void AssignTool(ToolComparisonItem? item)
        {
            if (item == null) return;

            string station = item.Station;
            string korrektur = item.Korrektur;
            string? filterArt = item.BearbeitungsArt; // NEU

            // Navigation zum ToolManagementViewModel (Auswahlmodus)
            // --- KORREKTUR HINZUGEFÜGT: _mainViewModel als erstes Argument übergeben ---
            var toolManagementVM = new ToolManagementViewModel(_mainViewModel, selectedTool =>
            {
                // Callback-Aktion nach Auswahl des Werkzeugs
                PerformToolAssignmentUpdate(station, korrektur, selectedTool.WerkzeugID);

                // Zurück zur Vergleichsansicht
                _mainViewModel.NavigateBack();
            }, filterArt); // NEU: filterArt übergeben

            _mainViewModel.NavigateTo(toolManagementVM);
        }

        // Logik zum Speichern der Zuweisung in der Datenbank
        private void PerformToolAssignmentUpdate(string station, string korrektur, int werkzeugId)
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
            }
            context.SaveChanges();

            // Lade die Daten in der Vergleichsansicht neu, um die UI zu aktualisieren
            LoadComparisonData();

            // NEU: Benachrichtigt den Fertig-Button über den geänderten Status
            FinishAssignmentCommand.NotifyCanExecuteChanged();
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