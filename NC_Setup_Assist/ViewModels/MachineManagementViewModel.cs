// NC_Setup_Assist/ViewModels/MachineManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MachineManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private Maschine? _machineToDeleteOnCancel;

        // NEU: Speichert den Zustand der Standardwerkzeuge vor der Bearbeitung
        private List<StandardWerkzeugZuweisung>? _originalStandardTools;


        public ObservableCollection<Maschine> Maschinen { get; } = new();
        public ObservableCollection<Hersteller> Hersteller { get; } = new();
        public ObservableCollection<Standort> Standorte { get; } = new();

        [ObservableProperty]
        private Maschine? _selectedMaschine;

        [ObservableProperty]
        private Maschine? _editingMaschine;

        [ObservableProperty]
        private bool _isInEditMode;

        public MachineManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadData();
        }

        private void LoadData()
        {
            Maschinen.Clear();
            Hersteller.Clear();
            Standorte.Clear();

            using var context = new NcSetupContext();
            var maschinenFromDb = context.Maschinen
                                         .Include(m => m.Hersteller)
                                         .Include(m => m.ZugehoerigerStandort)
                                         .ToList();
            foreach (var maschine in maschinenFromDb)
            {
                Maschinen.Add(maschine);
            }

            var herstellerFromDb = context.Hersteller.OrderBy(h => h.Name).ToList();
            foreach (var herst in herstellerFromDb)
            {
                Hersteller.Add(herst);
            }

            var standorteFromDb = context.Standorte.ToList();
            foreach (var standort in standorteFromDb)
            {
                Standorte.Add(standort);
            }
        }

        private void RefreshDataAndEditingState()
        {
            // IDs der aktuellen Auswahl merken (falls vorhanden)
            int? editingMachineId = EditingMaschine?.MaschineID;
            var selectedHerstellerId = EditingMaschine?.Hersteller?.HerstellerID;
            var selectedStandortId = EditingMaschine?.ZugehoerigerStandort?.StandortID;

            // Alle Daten neu aus der DB laden
            LoadData();

            // Wenn wir im Bearbeitungsmodus sind, die Auswahl wiederherstellen
            if (EditingMaschine != null)
            {
                // Auswahl für Hersteller wiederherstellen
                if (selectedHerstellerId.HasValue)
                {
                    EditingMaschine.Hersteller = Hersteller.FirstOrDefault(h => h.HerstellerID == selectedHerstellerId.Value);
                }

                // Auswahl für Standort wiederherstellen
                if (selectedStandortId.HasValue)
                {
                    EditingMaschine.ZugehoerigerStandort = Standorte.FirstOrDefault(s => s.StandortID == selectedStandortId.Value);
                }

                // Daten der Maschine selbst (wie Stationsanzahl) aktualisieren
                if (editingMachineId.HasValue && editingMachineId != 0)
                {
                    var refreshedMachineInList = Maschinen.FirstOrDefault(m => m.MaschineID == editingMachineId.Value);
                    if (refreshedMachineInList != null)
                    {
                        EditingMaschine.AnzahlStationen = refreshedMachineInList.AnzahlStationen;
                    }
                }
            }
        }


        [RelayCommand]
        private void NewMachine()
        {
            EditingMaschine = new Maschine();
            IsInEditMode = true;
            _machineToDeleteOnCancel = null;
            _originalStandardTools = null; // Sicherstellen, dass kein Backup aktiv ist
        }

        [RelayCommand]
        private void EditMachine()
        {
            if (SelectedMaschine == null) return;

            EditingMaschine = new Maschine
            {
                MaschineID = SelectedMaschine.MaschineID,
                Name = SelectedMaschine.Name,
                Seriennummer = SelectedMaschine.Seriennummer,
                AnzahlStationen = SelectedMaschine.AnzahlStationen,
                HerstellerID = SelectedMaschine.HerstellerID,
                StandortID = SelectedMaschine.StandortID,
                Hersteller = SelectedMaschine.Hersteller,
                ZugehoerigerStandort = SelectedMaschine.ZugehoerigerStandort
            };
            IsInEditMode = true;
            _machineToDeleteOnCancel = null;
            _originalStandardTools = null; // Sicherstellen, dass kein Backup aktiv ist
        }


        [RelayCommand]
        private void SaveMachine()
        {
            if (EditingMaschine == null ||
                string.IsNullOrWhiteSpace(EditingMaschine.Name) ||
                EditingMaschine.Hersteller == null ||
                EditingMaschine.ZugehoerigerStandort == null)
            {
                MessageBox.Show("Bitte füllen Sie alle erforderlichen Felder aus (Name, Hersteller, Standort).", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var context = new NcSetupContext();
            if (EditingMaschine.MaschineID == 0) // Neue Maschine
            {
                EditingMaschine.HerstellerID = EditingMaschine.Hersteller.HerstellerID;
                EditingMaschine.StandortID = EditingMaschine.ZugehoerigerStandort.StandortID;

                EditingMaschine.Hersteller = null!;
                EditingMaschine.ZugehoerigerStandort = null!;

                context.Maschinen.Add(EditingMaschine);
            }
            else // Bestehende Maschine aktualisieren
            {
                var machineToUpdate = context.Maschinen.Find(EditingMaschine.MaschineID);
                if (machineToUpdate != null)
                {
                    machineToUpdate.Name = EditingMaschine.Name;
                    machineToUpdate.Seriennummer = EditingMaschine.Seriennummer;
                    // AnzahlStationen wurde bereits in StandardToolsManagementViewModel gespeichert
                    machineToUpdate.AnzahlStationen = EditingMaschine.AnzahlStationen;

                    machineToUpdate.HerstellerID = EditingMaschine.Hersteller != null
                        ? EditingMaschine.Hersteller.HerstellerID
                        : EditingMaschine.HerstellerID;
                    machineToUpdate.StandortID = EditingMaschine.ZugehoerigerStandort != null
                        ? EditingMaschine.ZugehoerigerStandort.StandortID
                        : EditingMaschine.StandortID;
                }
            }
            context.SaveChanges();

            // NEU: Backup löschen, da Speicherung erfolgt ist
            _originalStandardTools = null;

            _machineToDeleteOnCancel = null;

            IsInEditMode = false;
            EditingMaschine = null;
            LoadData();
        }

        [RelayCommand]
        private void DeleteMachine()
        {
            if (SelectedMaschine == null) return;

            var result = MessageBox.Show($"Möchten Sie die Maschine '{SelectedMaschine.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var context = new NcSetupContext();
                var machineToDelete = context.Maschinen.Find(SelectedMaschine.MaschineID);
                if (machineToDelete != null)
                {
                    context.Maschinen.Remove(machineToDelete);
                    context.SaveChanges();
                }
                LoadData();
            }
        }

        [RelayCommand]
        private void ManageStandardTools()
        {
            if (EditingMaschine == null ||
                string.IsNullOrWhiteSpace(EditingMaschine.Name) ||
                EditingMaschine.Hersteller == null ||
                EditingMaschine.ZugehoerigerStandort == null)
            {
                MessageBox.Show("Bitte füllen Sie zuerst die Felder Name, Hersteller und Standort aus.", "Fehlende Daten", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingMaschine.MaschineID == 0)
            {
                // Logik für NEUE Maschine: Temporär speichern und Lösch-Flag setzen
                using var context = new NcSetupContext();

                EditingMaschine.HerstellerID = EditingMaschine.Hersteller.HerstellerID;
                EditingMaschine.StandortID = EditingMaschine.ZugehoerigerStandort.StandortID;

                var tempHersteller = EditingMaschine.Hersteller;
                var tempStandort = EditingMaschine.ZugehoerigerStandort;

                EditingMaschine.Hersteller = null!;
                EditingMaschine.ZugehoerigerStandort = null!;

                context.Maschinen.Add(EditingMaschine);
                context.SaveChanges();

                EditingMaschine.Hersteller = tempHersteller;
                EditingMaschine.ZugehoerigerStandort = tempStandort;

                _machineToDeleteOnCancel = EditingMaschine;
                _originalStandardTools = null; // Kein Revert nötig, wird kaskadiert gelöscht
            }
            // NEU: Backup für bestehende Maschine erstellen
            else
            {
                using var context = new NcSetupContext();
                // Lade die aktuellen Zuweisungen, um sie bei Abbruch wiederherzustellen
                _originalStandardTools = context.StandardWerkzeugZuweisungen
                    .AsNoTracking() // Wichtig: Nur lesen, nicht tracken
                    .Where(z => z.MaschineID == EditingMaschine.MaschineID)
                    .ToList();
            }

            _mainViewModel.NavigateTo(new StandardToolsManagementViewModel(_mainViewModel, EditingMaschine, RefreshDataAndEditingState));
        }

        [RelayCommand]
        private void Cancel()
        {
            // 1. Logik für NEUE Maschine: Temporäre Maschine löschen
            if (_machineToDeleteOnCancel != null)
            {
                using var context = new NcSetupContext();
                var machineToDelete = context.Maschinen.Find(_machineToDeleteOnCancel.MaschineID);
                if (machineToDelete != null)
                {
                    context.Maschinen.Remove(machineToDelete);
                    context.SaveChanges();
                }
            }

            // 2. NEU: Wiederherstellung der Standardwerkzeuge für BESTEHENDE Maschine
            if (EditingMaschine != null && EditingMaschine.MaschineID != 0 && _originalStandardTools != null)
            {
                using var context = new NcSetupContext();

                // a) Lösche alle Zuweisungen, die der Benutzer im Untermenü eventuell gespeichert hat
                var currentAssignments = context.StandardWerkzeugZuweisungen
                    .Where(z => z.MaschineID == EditingMaschine.MaschineID);
                context.StandardWerkzeugZuweisungen.RemoveRange(currentAssignments);

                // b) Füge die ursprünglichen (gesicherten) Zuweisungen wieder hinzu
                context.StandardWerkzeugZuweisungen.AddRange(_originalStandardTools);

                context.SaveChanges();
                _originalStandardTools = null; // Backup-Speicher leeren
            }

            EditingMaschine = null;
            IsInEditMode = false;
            _machineToDeleteOnCancel = null;
            LoadData();
        }

        [RelayCommand]
        private void AddHersteller()
        {
            // Die neue, intelligentere Refresh-Methode als Callback übergeben
            _mainViewModel.NavigateTo(new HerstellerManagementViewModel(RefreshDataAndEditingState));
        }
    }
}