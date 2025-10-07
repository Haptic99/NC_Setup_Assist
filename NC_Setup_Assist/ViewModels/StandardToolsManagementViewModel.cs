// NC_Setup_Assist/ViewModels/StandardToolsManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace NC_Setup_Assist.ViewModels
{
    // Ein kleines Hilfs-ViewModel, um jede Zeile in unserer Liste zu repräsentieren
    public partial class StandardToolAssignmentViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Werkzeug? _selectedWerkzeug;

        public int Station { get; }

        public StandardToolAssignmentViewModel(int station, Werkzeug? assignedTool)
        {
            Station = station;
            _selectedWerkzeug = assignedTool;
        }

        // Command zum Löschen des zugewiesenen Werkzeugs
        [RelayCommand]
        private void ClearTool()
        {
            SelectedWerkzeug = null;
        }
    }


    public partial class StandardToolsManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Maschine _machine;

        // Callback zum Zurückgeben der UNSICHEREN Änderungen (Liste der Zuweisungen und Stationsanzahl)
        private readonly Action<List<StandardWerkzeugZuweisung>, int> _onSaveCallback;
        private readonly Action _onCancelCallback;

        // NEU: Feld für die übergebenen, unsicheren Änderungen
        private readonly List<StandardWerkzeugZuweisung>? _initialPendingAssignments;

        [ObservableProperty]
        private int _selectedAnzahlStationen;

        [ObservableProperty]
        private bool _isAnzahlStationenChangeable;

        public List<int> StationOptions { get; } = Enumerable.Range(4, 21).ToList(); // Zahlen von 4 bis 24
        public ObservableCollection<StandardToolAssignmentViewModel> ToolAssignments { get; } = new();

        // Angepasster Konstruktor
        public StandardToolsManagementViewModel(
            MainViewModel mainViewModel,
            Maschine machine,
            Action<List<StandardWerkzeugZuweisung>, int> onSaveCallback,
            Action onCancelCallback,
            // NEU: Optionaler Parameter für pending changes
            List<StandardWerkzeugZuweisung>? initialPendingAssignments = null)
        {
            _mainViewModel = mainViewModel;
            _machine = machine;
            // Callbacks übergeben
            _onSaveCallback = onSaveCallback;
            _onCancelCallback = onCancelCallback;
            // NEU: Initialisiere das Feld
            _initialPendingAssignments = initialPendingAssignments;

            // Lade die Tools (entweder aus Pending-Liste oder DB)
            LoadStandardTools();

            // Setze die Stationsanzahl basierend auf der Maschine (die ggf. schon Pending-Werte enthält)
            SelectedAnzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12;

            // Führe die Logik für die Stationsanzahl aus, um die Liste an die Größe anzupassen
            if (ToolAssignments.Count != SelectedAnzahlStationen)
            {
                OnSelectedAnzahlStationenChanged(SelectedAnzahlStationen);
            }

            // Die Anzahl der Stationen ist nur änderbar, wenn noch KEIN Werkzeug zugewiesen ist.
            IsAnzahlStationenChangeable = !ToolAssignments.Any(t => t.SelectedWerkzeug != null);
        }

        private void LoadStandardTools()
        {
            ToolAssignments.Clear();

            // 1. Hole die tatsächliche Stationsanzahl (entweder aus der DB-Maschine oder dem Pending-State des Parent-VM)
            int anzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12;

            // 2. Lade die Zuweisungen
            List<StandardWerkzeugZuweisung> assignmentsToUse;

            if (_initialPendingAssignments != null)
            {
                // FALL A (FIX): Verwende die übergebenen, unsicheren Zuweisungen aus dem Parent-VM
                assignmentsToUse = _initialPendingAssignments;

                // Füge die Navigationseigenschaften hinzu, falls sie fehlen (dies ist notwendig für die Anzeige in der UI)
                using var context = new NcSetupContext();
                foreach (var assignment in assignmentsToUse)
                {
                    if (assignment.ZugehoerigesWerkzeug == null)
                    {
                        // Lade das Werkzeug direkt aus der DB, da es im Pending-Objekt nur als ID existiert
                        assignment.ZugehoerigesWerkzeug = context.Werkzeuge
                            .Include(w => w.Unterkategorie)
                            .SingleOrDefault(w => w.WerkzeugID == assignment.WerkzeugID);
                    }
                }
            }
            else
            {
                // FALL B: Lade Zuweisungen aus der Datenbank (ursprüngliches Verhalten)
                using var context = new NcSetupContext();
                assignmentsToUse = context.StandardWerkzeugZuweisungen
                    .Where(z => z.MaschineID == _machine.MaschineID)
                    .Include(z => z.ZugehoerigesWerkzeug)
                    .ThenInclude(w => w!.Unterkategorie)
                    .ToList();
            }

            // 3. Fülle die ViewModel-Liste
            // Erstelle ein schnelles Lookup für die Zuweisungen (Schlüssel: RevolverStation, Wert: Werkzeug)
            var assignmentLookup = assignmentsToUse
                .Where(a => a.ZugehoerigesWerkzeug != null)
                .ToDictionary(a => a.RevolverStation, a => a.ZugehoerigesWerkzeug);

            // Nur bis zur aktuellen Stationsanzahl iterieren
            for (int i = 1; i <= anzahlStationen; i++)
            {
                assignmentLookup.TryGetValue(i, out var assignedTool);
                ToolAssignments.Add(new StandardToolAssignmentViewModel(i, assignedTool));
            }
        }

        // Wird aufgerufen, wenn die Anzahl der Stationen im Dropdown geändert wird
        partial void OnSelectedAnzahlStationenChanged(int value)
        {
            // Behalte die bereits zugewiesenen Werkzeuge (falls die neue Größe dies zulässt)
            var existingAssignments = ToolAssignments
                .Where(a => a.SelectedWerkzeug != null && a.Station <= value)
                .ToDictionary(a => a.Station, a => a.SelectedWerkzeug);

            ToolAssignments.Clear();
            for (int i = 1; i <= value; i++)
            {
                existingAssignments.TryGetValue(i, out var existingTool);
                ToolAssignments.Add(new StandardToolAssignmentViewModel(i, existingTool));
            }
        }

        [RelayCommand]
        private void ChooseTool(StandardToolAssignmentViewModel? assignmentVM)
        {
            if (assignmentVM == null) return;

            var toolManagementVM = new ToolManagementViewModel(selectedTool =>
            {
                assignmentVM.SelectedWerkzeug = selectedTool;
                _mainViewModel.NavigateBack();
                // Wenn ein Werkzeug ausgewählt/zugewiesen wurde, kann die Stationsanzahl nicht mehr geändert werden.
                IsAnzahlStationenChangeable = false;
            });

            _mainViewModel.NavigateTo(toolManagementVM);
        }

        [RelayCommand]
        private void Save()
        {
            // Änderungen in ein temporäres Listenobjekt konvertieren (KEIN DB-SAVE!)
            var updatedAssignments = new List<StandardWerkzeugZuweisung>();

            // 1. Erzeuge die unsicheren Zuweisungen
            foreach (var assignmentVM in ToolAssignments)
            {
                if (assignmentVM.SelectedWerkzeug != null)
                {
                    updatedAssignments.Add(new StandardWerkzeugZuweisung
                    {
                        // Wichtig: MaschineID wird für die korrekte Zuordnung im Parent-ViewModel benötigt
                        MaschineID = _machine.MaschineID,
                        RevolverStation = assignmentVM.Station,
                        // Speichere nur die ID, da die eigentliche Entität bereits in der DB existiert
                        WerkzeugID = assignmentVM.SelectedWerkzeug.WerkzeugID
                    });
                }
            }

            // 2. Rufe das Callback im Parent-ViewModel auf und übergib die unsicheren Daten
            _onSaveCallback?.Invoke(updatedAssignments, SelectedAnzahlStationen);

            // 3. Zurück zum MachineManagementViewModel
            _mainViewModel.NavigateBack();
        }

        [RelayCommand]
        private void Cancel()
        {
            // Verwirf alle lokalen Änderungen und gehe zurück
            _mainViewModel.NavigateBack();
        }
    }
}