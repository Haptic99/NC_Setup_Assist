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
    }


    public partial class StandardToolsManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Maschine _machine;
        private readonly Action? _onCloseCallback; // Hinzugefügte Callback-Funktion

        [ObservableProperty]
        private int _selectedAnzahlStationen;

        [ObservableProperty]
        private bool _isAnzahlStationenChangeable;

        public List<int> StationOptions { get; } = Enumerable.Range(4, 21).ToList(); // Zahlen von 4 bis 24
        public ObservableCollection<StandardToolAssignmentViewModel> ToolAssignments { get; } = new();

        // Angepasster Konstruktor
        public StandardToolsManagementViewModel(MainViewModel mainViewModel, Maschine machine, Action? onCloseCallback = null)
        {
            _mainViewModel = mainViewModel;
            _machine = machine;
            _onCloseCallback = onCloseCallback; // Callback speichern

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
            // Behalte die bereits zugewiesenen Werkzeuge
            var existingAssignments = ToolAssignments
                .Where(a => a.SelectedWerkzeug != null)
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
            using var context = new NcSetupContext();

            // 1. Aktualisiere die Anzahl der Stationen auf der Maschine selbst
            var machineToUpdate = context.Maschinen.Find(_machine.MaschineID);
            if (machineToUpdate != null)
            {
                machineToUpdate.AnzahlStationen = SelectedAnzahlStationen;
            }

            // 2. Lösche alle bisherigen Zuweisungen für diese Maschine
            var existingAssignments = context.StandardWerkzeugZuweisungen
                .Where(z => z.MaschineID == _machine.MaschineID);
            context.StandardWerkzeugZuweisungen.RemoveRange(existingAssignments);

            // 3. Füge die neuen Zuweisungen aus der aktuellen Ansicht hinzu
            foreach (var assignmentVM in ToolAssignments)
            {
                if (assignmentVM.SelectedWerkzeug != null)
                {
                    var newAssignment = new StandardWerkzeugZuweisung
                    {
                        MaschineID = _machine.MaschineID,
                        RevolverStation = assignmentVM.Station,
                        WerkzeugID = assignmentVM.SelectedWerkzeug.WerkzeugID
                    };
                    context.StandardWerkzeugZuweisungen.Add(newAssignment);
                }
            }

            context.SaveChanges();
            _onCloseCallback?.Invoke(); // Callback hier aufrufen
            _mainViewModel.NavigateBack();
        }

        [RelayCommand]
        private void Cancel()
        {
            _onCloseCallback?.Invoke(); // Callback hier aufrufen
            _mainViewModel.NavigateBack();
        }
    }
}