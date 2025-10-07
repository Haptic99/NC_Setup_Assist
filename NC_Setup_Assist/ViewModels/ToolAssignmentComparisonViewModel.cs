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

        // Indikator für die Zuweisung: True, wenn manuell zugewiesen ODER als Standardwerkzeug erkannt.
        public bool IsAssigned => ToolNameAfter != "Unzugewiesen";

        public ToolComparisonItem(string station, string korrektur, string before, string after)
        {
            Station = station;
            Korrektur = korrektur;
            ToolNameBefore = before;
            ToolNameAfter = after;
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
        }

        private void LoadComparisonData()
        {
            ComparisonItems.Clear();

            using var context = new NcSetupContext();

            // 1. Lade Standardwerkzeuge der Maschine (für "Before"-Status und als Fallback für "After"-Status)
            var standardTools = context.StandardWerkzeugZuweisungen
                                        .Where(z => z.MaschineID == _programm.MaschineID)
                                        .Include(z => z.ZugehoerigesWerkzeug)
                                            .ThenInclude(w => w!.Unterkategorie)
                                        .ToList();

            var standardToolLookup = standardTools.ToDictionary(
                z => z.RevolverStation.ToString(),
                z => z.ZugehoerigesWerkzeug
            );

            // 2. Lade alle eindeutigen Werkzeugeinsätze des Programms, diesmal DIREKT aus der DB,
            // um den aktuellen Zuweisungsstatus zu sehen.
            var allUniqueTools = context.WerkzeugEinsaetze
                .Where(e => e.NCProgrammID == _programm.NCProgrammID && !string.IsNullOrEmpty(e.RevolverStation))
                .Include(e => e.ZugehoerigesWerkzeug) // Lade das aktuell zugewiesene Werkzeug
                .Select(e => new { e.RevolverStation, e.KorrekturNummer, ZugewiesenesWerkzeug = e.ZugehoerigesWerkzeug })
                .AsEnumerable() // Wechsle zu In-Memory-Verarbeitung
                .GroupBy(e => new { e.RevolverStation, e.KorrekturNummer })
                .Select(g => g.First())
                .ToList();


            // 3. Verarbeite die Vergleichsdaten
            foreach (var uniqueTool in allUniqueTools.OrderBy(t => t.RevolverStation).ThenBy(t => t.KorrekturNummer))
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
                if (uniqueTool.ZugewiesenesWerkzeug != null)
                {
                    // 1. Manuelle Zuweisung in DB gefunden (höchste Priorität)
                    toolAfterName = uniqueTool.ZugewiesenesWerkzeug.Name;
                }
                else
                {
                    // 2. Keine manuelle Zuweisung, prüfe auf Standardwerkzeug (Fall-Through von toolBeforeName)
                    if (stdTool != null)
                    {
                        // Standardwerkzeug gefunden -> Wird als zugewiesen betrachtet.
                        toolAfterName = $"{stdTool.Name}";
                    }
                    else
                    {
                        // 3. Weder manuell zugewiesen noch Standard
                        toolAfterName = "Unzugewiesen";
                    }
                }

                ComparisonItems.Add(new ToolComparisonItem(
                    uniqueTool.RevolverStation!,
                    korrekturKey,
                    toolBeforeName,
                    toolAfterName
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

            // Navigation zum ToolManagementViewModel (Auswahlmodus)
            var toolManagementVM = new ToolManagementViewModel(selectedTool =>
            {
                // Callback-Aktion nach Auswahl des Werkzeugs
                PerformToolAssignmentUpdate(station, korrektur, selectedTool.WerkzeugID);

                // Zurück zur Vergleichsansicht
                _mainViewModel.NavigateBack();
            });

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
            }
            context.SaveChanges();

            // Lade die Daten in der Vergleichsansicht neu, um die UI zu aktualisieren
            LoadComparisonData();
        }

        [RelayCommand]
        private void Close()
        {
            _mainViewModel.NavigateBack();
        }
    }
}