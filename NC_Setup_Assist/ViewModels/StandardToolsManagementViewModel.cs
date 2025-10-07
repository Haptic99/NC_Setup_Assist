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

        // NEU: Callback zum Zurückgeben der UNSICHEREN Änderungen (Liste der Zuweisungen und Stationsanzahl)
        private readonly Action<List<StandardWerkzeugZuweisung>, int> _onSaveCallback;
        private readonly Action _onCancelCallback;

        [ObservableProperty]
        private int _selectedAnzahlStationen;

        [ObservableProperty]
        private bool _isAnzahlStationenChangeable;

        public List<int> StationOptions { get; } = Enumerable.Range(4, 21).ToList(); // Zahlen von 4 bis 24
        public ObservableCollection<StandardToolAssignmentViewModel> ToolAssignments { get; } = new();

        // Angepasster Konstruktor
        public StandardToolsManagementViewModel(MainViewModel mainViewModel, Maschine machine, Action<List<StandardWerkzeugZuweisung>, int> onSaveCallback, Action onCancelCallback)
        {
            _mainViewModel = mainViewModel;
            _machine = machine;
            // NEU: Callbacks übergeben
            _onSaveCallback = onSaveCallback;
            _onCancelCallback = onCancelCallback;

            // Lade die aktuellen Daten der Maschine, um die Stationsanzahl korrekt zu setzen
            LoadStandardTools();

            SelectedAnzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12; // Standard: 12, falls nichts gesetzt

            // Die Anzahl der Stationen ist nur änderbar, wenn noch KEIN Werkzeug zugewiesen ist.
            IsAnzahlStationenChangeable = !ToolAssignments.Any(t => t.SelectedWerkzeug != null);
        }

        private void LoadStandardTools()
        {
            ToolAssignments.Clear();
            using var context = new NcSetupContext();
            var standardTools = context.StandardWerkzeugZuweisungen
                .Where(z => z.MaschineID == _machine.MaschineID)
                .Include(z => z.ZugehoerigesWerkzeug)
                .ThenInclude(w => w!.Unterkategorie)
                .ToList();

            // Beziehe die Anzahl der Stationen aus der Datenbank-Instanz der Maschine
            int anzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12;

            for (int i = 1; i <= anzahlStationen; i++)
            {
                var assignment = standardTools.FirstOrDefault(t => t.RevolverStation == i);
                ToolAssignments.Add(new StandardToolAssignmentViewModel(i, assignment?.ZugehoerigesWerkzeug));
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
            });

            _mainViewModel.NavigateTo(toolManagementVM);
        }

        [RelayCommand]
        private void Save()
        {
            // NEU: Änderungen in ein temporäres Listenobjekt konvertieren (KEIN DB-SAVE!)
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