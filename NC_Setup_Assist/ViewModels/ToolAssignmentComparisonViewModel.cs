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
            List<WerkzeugEinsatz> finalAssignments)
        {
            _mainViewModel = mainViewModel;
            _programm = programm;

            LoadComparisonData(finalAssignments);
        }

        private void LoadComparisonData(List<WerkzeugEinsatz> finalAssignments)
        {
            ComparisonItems.Clear();

            using var context = new NcSetupContext();

            // 1. Lade Standardwerkzeuge der Maschine (für "Before"-Status)
            var standardTools = context.StandardWerkzeugZuweisungen
                                        .Where(z => z.MaschineID == _programm.MaschineID)
                                        .Include(z => z.ZugehoerigesWerkzeug)
                                            .ThenInclude(w => w!.Unterkategorie)
                                        .ToList();

            var standardToolLookup = standardTools.ToDictionary(
                z => z.RevolverStation.ToString(),
                z => z.ZugehoerigesWerkzeug
            );

            // 2. Erzeuge eine Liste aller EINDEUTIGEN Werkzeug-Keys (Station + Korrektur), die jemals im Programm aufgerufen wurden
            var allUniqueTools = context.WerkzeugEinsaetze
                .Where(e => e.NCProgrammID == _programm.NCProgrammID && !string.IsNullOrEmpty(e.RevolverStation))
                .Select(e => new { e.RevolverStation, e.KorrekturNummer })
                .Distinct()
                .ToList();

            // 3. Verarbeite die Vergleichsdaten
            foreach (var uniqueTool in allUniqueTools.OrderBy(t => t.RevolverStation).ThenBy(t => t.KorrekturNummer))
            {
                string stationKey = uniqueTool.RevolverStation!.TrimStart('0');
                string korrekturKey = uniqueTool.KorrekturNummer ?? "";

                // --- Bestimme "Before" Status (Standardwerkzeug) ---
                string toolBeforeName = "Unbekannt";
                if (standardToolLookup.TryGetValue(stationKey, out var stdTool))
                {
                    toolBeforeName = $"{stdTool.Name} (Standard)";
                }

                // --- Bestimme "After" Status (Final zugewiesen) ---
                var finalAssignment = finalAssignments.FirstOrDefault(a =>
                    a.RevolverStation == uniqueTool.RevolverStation &&
                    (a.KorrekturNummer == korrekturKey || (string.IsNullOrEmpty(korrekturKey) && a.KorrekturNummer == null)));

                string toolAfterName = "Unzugewiesen";
                if (finalAssignment?.WerkzeugID != null)
                {
                    var assignedTool = context.Werkzeuge
                        .SingleOrDefault(w => w.WerkzeugID == finalAssignment.WerkzeugID);

                    if (assignedTool != null)
                    {
                        toolAfterName = assignedTool.Name;
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

        [RelayCommand]
        private void Close()
        {
            _mainViewModel.NavigateBack();
        }
    }
}