using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
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

        // --- NEU ---
        public ICollectionView FilteredToolsView { get; }

        [ObservableProperty]
        private string? _searchText;

        public StandardToolAssignmentViewModel(int station, ObservableCollection<Werkzeug> allTools, Werkzeug? assignedTool)
        {
            Station = station;
            _selectedWerkzeug = assignedTool;

            // --- NEU ---
            // Erstellt eine Kollektion, die eine leere Option (null) am Anfang enthält.
            var toolsWithEmptyOption = new ObservableCollection<Werkzeug?> { null };
            allTools.ToList().ForEach(t => toolsWithEmptyOption.Add(t));

            FilteredToolsView = CollectionViewSource.GetDefaultView(toolsWithEmptyOption);
            FilteredToolsView.Filter = FilterTools;
        }

        // --- NEU ---
        partial void OnSearchTextChanged(string? value)
        {
            FilteredToolsView.Refresh();
        }

        // --- NEU ---
        private bool FilterTools(object item)
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return true; // Kein Filter, alles anzeigen
            }

            if (item is Werkzeug tool)
            {
                // Stellt sicher, dass die Eigenschaft 'Name' nicht null ist, bevor 'Contains' aufgerufen wird
                return tool.Name?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false;
            }

            return item == null; // Die leere Option immer anzeigen
        }
    }


    public partial class StandardToolsManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Maschine _machine;

        [ObservableProperty]
        private int _selectedAnzahlStationen;

        [ObservableProperty]
        private bool _isAnzahlStationenChangeable;

        public List<int> StationOptions { get; } = Enumerable.Range(4, 21).ToList(); // Zahlen von 4 bis 24
        public ObservableCollection<StandardToolAssignmentViewModel> ToolAssignments { get; } = new();
        public ObservableCollection<Werkzeug> AllTools { get; } = new();

        public StandardToolsManagementViewModel(MainViewModel mainViewModel, Maschine machine)
        {
            _mainViewModel = mainViewModel;
            _machine = machine;

            LoadAllTools();
            LoadStandardTools();

            SelectedAnzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12; // Standard: 12, falls nichts gesetzt
            // Die Anzahl der Stationen ist nur änderbar, wenn noch KEIN Werkzeug zugewiesen ist.
            IsAnzahlStationenChangeable = !ToolAssignments.Any(t => t.SelectedWerkzeug != null);
        }

        private void LoadAllTools()
        {
            AllTools.Clear();
            using var context = new NcSetupContext();
            var tools = context.Werkzeuge.OrderBy(t => t.Name).ToList();
            foreach (var tool in tools)
            {
                AllTools.Add(tool);
            }
        }

        private void LoadStandardTools()
        {
            ToolAssignments.Clear();
            using var context = new NcSetupContext();
            var standardTools = context.StandardWerkzeugZuweisungen
                .Where(z => z.MaschineID == _machine.MaschineID)
                .Include(z => z.ZugehoerigesWerkzeug)
                .ToList();

            int anzahlStationen = _machine.AnzahlStationen >= 4 ? _machine.AnzahlStationen : 12;

            for (int i = 1; i <= anzahlStationen; i++)
            {
                // Finde die gespeicherte Zuweisung für die aktuelle Station
                var assignment = standardTools.FirstOrDefault(t => t.RevolverStation == i);

                // Finde das Werkzeug-Objekt aus der Hauptliste "AllTools" anhand der ID
                var assignedTool = assignment != null
                    ? AllTools.FirstOrDefault(t => t.WerkzeugID == assignment.WerkzeugID)
                    : null;

                // Erstelle die Zeile mit der korrekten Werkzeug-Instanz
                ToolAssignments.Add(new StandardToolAssignmentViewModel(i, AllTools, assignedTool));
            }
        }

        // Wird aufgerufen, wenn die Anzahl der Stationen im Dropdown geändert wird
        partial void OnSelectedAnzahlStationenChanged(int value)
        {
            // Behalte die bereits zugewiesenen Werkzeuge
            var existingAssignments = ToolAssignments
                .Where(a => a.SelectedWerkzeug != null)
                .ToList();

            ToolAssignments.Clear();
            for (int i = 1; i <= value; i++)
            {
                var existing = existingAssignments.FirstOrDefault(a => a.Station == i);
                ToolAssignments.Add(new StandardToolAssignmentViewModel(i, AllTools, existing?.SelectedWerkzeug));
            }
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
            _mainViewModel.NavigateBack();
        }

        [RelayCommand]
        private void Cancel()
        {
            _mainViewModel.NavigateBack();
        }
    }
}