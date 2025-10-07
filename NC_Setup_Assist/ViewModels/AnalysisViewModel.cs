using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class AnalysisViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly NCProgramm _currentProgramm;

        [ObservableProperty]
        private string? _ncCodeContent;

        public ObservableCollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; } = new();

        public ObservableCollection<Standort> Standorte { get; } = new();
        public ObservableCollection<Maschine> Maschinen { get; } = new();

        [ObservableProperty]
        private Standort? _selectedStandort;

        [ObservableProperty]
        private Maschine? _selectedMaschine;

        // --- NEUES FELD FÜR DEN ZUSTAND DER ZUWEISUNG ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartToolAssignmentCommand))]
        private bool _canStartToolAssignment;
        // --------------------------------------------------

        // --- NEUES FELD ZUM SPEICHERN DER LETZTEN SUCHE ---
        private readonly Dictionary<int, int> _lastSearchIndex = new();

        // --- NEUES EREIGNIS, UM DIE ANSICHT ZU BENACHRICHTIGEN ---
        public event Action<int, int>? RequestScrollAndSelect;

        public AnalysisViewModel(NCProgramm ncProgramm, MainViewModel mainViewModel)
        {
            _currentProgramm = ncProgramm;
            _mainViewModel = mainViewModel;

            LoadNcFileContent();
            LoadWerkzeugEinsaetze();
            LoadStandorte();
        }

        partial void OnSelectedStandortChanged(Standort? value)
        {
            LoadMaschinen(value);
        }

        // --- NEUER BEFEHL FÜR DEN DOPPELKLICK ---
        [RelayCommand]
        private void GoToToolInCode(WerkzeugEinsatz? werkzeugEinsatz)
        {
            if (werkzeugEinsatz == null || string.IsNullOrEmpty(NcCodeContent))
            {
                return;
            }

            // Muss gültige RevolverStation für die Suche haben
            if (string.IsNullOrEmpty(werkzeugEinsatz.RevolverStation))
            {
                return;
            }

            // Erzeugt einen eindeutigen String für das Werkzeug, z.B. "T0101"
            string korrekturNummer = werkzeugEinsatz.KorrekturNummer ?? "";

            // Verwendet die im NC-Code erwartete Formatierung Tnnkk, wobei nn die Revolverstation ist
            string toolIdentifier = $"T{werkzeugEinsatz.RevolverStation}{korrekturNummer}";

            int startIndex = 0;
            if (_lastSearchIndex.ContainsKey(werkzeugEinsatz.WerkzeugEinsatzID))
            {
                startIndex = _lastSearchIndex[werkzeugEinsatz.WerkzeugEinsatzID] + 1;
            }

            int foundIndex = NcCodeContent.IndexOf(toolIdentifier, startIndex, StringComparison.OrdinalIgnoreCase);

            // Wenn nichts gefunden wurde, fangen Sie von vorne an
            if (foundIndex == -1)
            {
                _lastSearchIndex.Remove(werkzeugEinsatz.WerkzeugEinsatzID);
                foundIndex = NcCodeContent.IndexOf(toolIdentifier, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (foundIndex != -1)
            {
                _lastSearchIndex[werkzeugEinsatz.WerkzeugEinsatzID] = foundIndex;

                // Löst das Ereignis aus, um die Ansicht zu benachrichtigen
                RequestScrollAndSelect?.Invoke(foundIndex, toolIdentifier.Length);
            }
        }

        private void LoadNcFileContent()
        {
            // Pfad aus der Benutzereingabe verwenden, wenn die Datei nicht am ursprünglichen Ort ist
            string filePath = _currentProgramm.DateiPfad;
            if (!File.Exists(filePath))
            {
                filePath = @"C:\Users\dmart\OneDrive\Arbeit\Firmen\STB Maschinenbau AG\Einrichtblatt Dreherei\Automatisch Einrichtblatt Dreherei\Programme\SC02312006.nc";
            }

            if (File.Exists(filePath))
            {
                NcCodeContent = File.ReadAllText(filePath);
            }
            else
            {
                NcCodeContent = $"Fehler: Die Datei konnte in keinem der Pfade gefunden werden:\n1. {_currentProgramm.DateiPfad}\n2. {filePath}";
            }
        }

        private void LoadWerkzeugEinsaetze()
        {
            WerkzeugEinsaetze.Clear();
            using var context = new NcSetupContext();

            // 1. Lade Standardwerkzeuge der Maschine in einen Lookup
            var standardTools = context.StandardWerkzeugZuweisungen
                                        .Where(z => z.MaschineID == _currentProgramm.MaschineID)
                                        .Include(z => z.ZugehoerigesWerkzeug)
                                            .ThenInclude(w => w!.Unterkategorie)
                                        .ToList();

            // Erstelle ein Lookup (Schlüssel: RevolverStation als string "1", "2", ...)
            // Wir nutzen die RevolverStation als Schlüssel, um geparste RevolverStation-Strings zu matchen.
            var standardToolLookup = standardTools.ToDictionary(
                z => z.RevolverStation.ToString(),
                z => z
            );


            // 2. Lade die durch den Parser gefundenen Werkzeugeinsätze
            var einsaetzeFromDb = context.WerkzeugEinsaetze
                                        .Where(e => e.NCProgrammID == _currentProgramm.NCProgrammID)
                                        // Laden Sie das Werkzeug, falls es schon manuell zugewiesen wurde
                                        .Include(e => e.ZugehoerigesWerkzeug)
                                            .ThenInclude(w => w!.Unterkategorie)
                                        .OrderBy(e => e.Reihenfolge)
                                        .ToList();

            // 3. Verknüpfe die geparsten Einsätze mit den Standardwerkzeugen (in memory)
            foreach (var einsatz in einsaetzeFromDb)
            {
                // Wenn das Werkzeug noch NICHT manuell zugewiesen ist (einsatz.WerkzeugID ist null)
                // und es eine Revolverstation gibt
                if (einsatz.WerkzeugID == null && !string.IsNullOrEmpty(einsatz.RevolverStation))
                {
                    // Versuche, das passende Standardwerkzeug zu finden
                    // Der RevolverStation-String des Parsers kann z.B. "01" oder "1" sein. Wir trimmen führende Nullen für den Match mit dem Int-Key.
                    string stationKey = einsatz.RevolverStation.TrimStart('0');
                    if (standardToolLookup.TryGetValue(stationKey, out var stdToolAssignment))
                    {
                        // Standardwerkzeug gefunden -> Verknüpfung in memory setzen
                        // Der WerkzeugID-Wert in der Datenbank bleibt NULL, solange der Benutzer das Werkzeug nicht manuell zuweist.
                        einsatz.ZugehoerigesWerkzeug = stdToolAssignment.ZugehoerigesWerkzeug;
                    }
                }

                WerkzeugEinsaetze.Add(einsatz);
            }

            // 4. Initialisiere CanStartToolAssignment
            CheckCanStartToolAssignment();
        }

        private void LoadStandorte()
        {
            Standorte.Clear();
            using var context = new NcSetupContext();
            var standorteFromDb = context.Standorte
                                         .Include(s => s.Maschinen)
                                         .ToList();
            foreach (var standort in standorteFromDb)
            {
                Standorte.Add(standort);
            }
        }

        private void LoadMaschinen(Standort? standort)
        {
            Maschinen.Clear();
            SelectedMaschine = null; // Auswahl zurücksetzen
            if (standort != null)
            {
                // Sammelt alle Maschinen vom ausgewählten Standort
                foreach (var maschine in standort.Maschinen)
                {
                    Maschinen.Add(maschine);
                }
            }
        }

        // --- NEUE LOGIK FÜR DIE SEQUENZIELLE ZUWEISUNG ---

        private void CheckCanStartToolAssignment()
        {
            // true, wenn noch eindeutige, unzugewiesene Werkzeuge vorhanden sind
            _canStartToolAssignment = WerkzeugEinsaetze
                .Where(e => e.WerkzeugID == null && !string.IsNullOrEmpty(e.RevolverStation))
                .GroupBy(e => new { e.RevolverStation, e.KorrekturNummer })
                .Any();

            StartToolAssignmentCommand.NotifyCanExecuteChanged();
        }

        private bool CanStartToolAssignment() => _canStartToolAssignment;

        [RelayCommand(CanExecute = nameof(CanStartToolAssignment))]
        private void StartToolAssignment()
        {
            AssignNextTool();
        }

        private void AssignNextTool()
        {
            // 1. Finde das nächste eindeutige, unzugewiesene Werkzeug
            var nextToolGroup = WerkzeugEinsaetze
                .Where(e => e.WerkzeugID == null && !string.IsNullOrEmpty(e.RevolverStation))
                .GroupBy(e => new { e.RevolverStation, e.KorrekturNummer })
                .OrderBy(g => g.Min(e => e.Reihenfolge)) // Nach Reihenfolge sortieren
                .FirstOrDefault();

            if (nextToolGroup == null)
            {
                // Zuweisung abgeschlossen
                CheckCanStartToolAssignment(); // Setzt CanExecute auf false

                // Navigation zum Vergleichsfenster
                // Es ist wichtig, die aktuelle Liste der Einsätze zu verwenden, da sie alle Informationen (auch die neuen Werkzeug-IDs) enthält
                var finalAssignments = WerkzeugEinsaetze.ToList();

                _mainViewModel.NavigateTo(new ToolAssignmentComparisonViewModel(_mainViewModel, _currentProgramm, finalAssignments));

                return;
            }

            var sampleEinsatz = nextToolGroup.First();
            string station = sampleEinsatz.RevolverStation!;
            string korrektur = sampleEinsatz.KorrekturNummer ?? "";

            // Navigation zum ToolManagementViewModel (Auswahlmodus)
            var toolManagementVM = new ToolManagementViewModel(selectedTool =>
            {
                // Callback-Aktion nach Auswahl des Werkzeugs
                PerformToolAssignmentUpdate(station, korrektur, selectedTool.WerkzeugID);

                // Setze den Prozess sofort fort (rekursiver Aufruf)
                _mainViewModel.NavigateBack(); // Schließt die ToolManagementView
                AssignNextTool();
            });

            MessageBox.Show($"Bitte weisen Sie ein Werkzeug für Revolverstation {station} und Korrektur {korrektur} zu. Dieses Werkzeug wird allen '{station}/{korrektur}' Einsätzen im Programm zugewiesen.", "Werkzeugzuweisung starten", MessageBoxButton.OK, MessageBoxImage.Information);

            _mainViewModel.NavigateTo(toolManagementVM);
        }

        private void PerformToolAssignmentUpdate(string station, string korrektur, int werkzeugId)
        {
            using var context = new NcSetupContext();

            // Finde ALLE WerkzeugEinsatz-Objekte mit der gleichen RevolverStation und KorrekturNummer im aktuellen NC-Programm
            var einsaetzeToUpdate = context.WerkzeugEinsaetze
                .Where(e => e.NCProgrammID == _currentProgramm.NCProgrammID &&
                            e.RevolverStation == station &&
                            (e.KorrekturNummer == korrektur || (string.IsNullOrEmpty(korrektur) && e.KorrekturNummer == null)))
                .ToList();

            // Aktualisiere die WerkzeugID in der Datenbank
            foreach (var einsatz in einsaetzeToUpdate)
            {
                einsatz.WerkzeugID = werkzeugId;
            }
            context.SaveChanges();

            // Lade die Daten im ViewModel neu, um die UI zu aktualisieren
            LoadWerkzeugEinsaetze();
        }

        // ------------------------------------------------------------------
    }
}