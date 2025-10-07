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

        // Hält die UNSICHEREN Änderungen der Standardwerkzeuge
        private List<StandardWerkzeugZuweisung>? _pendingStandardToolChanges;

        // Sichert und hält die Stationsanzahl
        private int _originalAnzahlStationen;
        private int? _pendingAnzahlStationen;


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

            // Pending- und Originalwerte zurücksetzen
            _pendingStandardToolChanges = null;
            _pendingAnzahlStationen = null;
            _originalAnzahlStationen = 0;
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

            // Stationsanzahl für Revert sichern und Pending-Werte zurücksetzen
            _originalAnzahlStationen = SelectedMaschine.AnzahlStationen;
            _pendingStandardToolChanges = null;
            _pendingAnzahlStationen = null;
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
            Maschine machineToSave;
            bool isNewMachine = EditingMaschine.MaschineID == 0;

            if (isNewMachine)
            {
                // Fallback für den Fall, dass ManageStandardTools nicht aufgerufen wurde (MaschineID = 0)
                // Wir erstellen eine saubere Entität nur mit benötigten Werten und FKs.
                machineToSave = new Maschine
                {
                    Name = EditingMaschine.Name,
                    Seriennummer = EditingMaschine.Seriennummer,
                    AnzahlStationen = _pendingAnzahlStationen ?? 0, // Stationsanzahl aus Pending oder Fallback
                    HerstellerID = EditingMaschine.Hersteller.HerstellerID,
                    StandortID = EditingMaschine.ZugehoerigerStandort.StandortID
                };
                context.Maschinen.Add(machineToSave);
                context.SaveChanges(); // Wichtig: Hier speichern, um die MachineID zu erhalten

                // Aktualisiere das VM-Objekt mit der neuen ID
                EditingMaschine.MaschineID = machineToSave.MaschineID;
            }
            else
            {
                // Existierende Maschine (oder temporär gespeicherte neue Maschine)
                // Finden und aktualisieren der verfolgten Entität
                machineToSave = context.Maschinen.Find(EditingMaschine.MaschineID)!;

                if (machineToSave == null) return; // Sollte nicht passieren

                // 1. Update Maschine Stammdaten
                machineToSave.Name = EditingMaschine.Name;
                machineToSave.Seriennummer = EditingMaschine.Seriennummer;
                machineToSave.HerstellerID = EditingMaschine.Hersteller.HerstellerID;
                machineToSave.StandortID = EditingMaschine.ZugehoerigerStandort.StandortID;

                // 2. Update Stationsanzahl aus pending oder aktuellem VM-Zustand
                machineToSave.AnzahlStationen = _pendingAnzahlStationen ?? EditingMaschine.AnzahlStationen;
            }

            // Aktualisiere EditingMaschine mit dem finalen Stationsstand
            EditingMaschine.AnzahlStationen = machineToSave.AnzahlStationen;

            // 3. Standardwerkzeug-Änderungen anwenden (für beide Maschinentypen)
            if (_pendingStandardToolChanges != null)
            {
                // Lösche alle bisherigen Zuweisungen für DIESE MASCHINE (ID ist jetzt garantiert gesetzt)
                var existingAssignments = context.StandardWerkzeugZuweisungen
                    .Where(z => z.MaschineID == machineToSave.MaschineID);
                context.StandardWerkzeugZuweisungen.RemoveRange(existingAssignments);

                // Füge die neuen Zuweisungen hinzu
                context.StandardWerkzeugZuweisungen.AddRange(_pendingStandardToolChanges);
            }

            // FINALER SAVE: Speichert alle ausstehenden Änderungen an der Maschine und den Zuweisungen
            if (context.ChangeTracker.HasChanges())
            {
                context.SaveChanges();
            }

            // Nach erfolgreichem Speichern: State zurücksetzen
            _pendingStandardToolChanges = null;
            _pendingAnzahlStationen = null;
            _originalAnzahlStationen = EditingMaschine!.AnzahlStationen;

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

            // 1. Wenn eine NEUE Maschine bearbeitet wird, muss diese temporär gespeichert werden, um eine ID zu erhalten
            if (EditingMaschine.MaschineID == 0)
            {
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
            }

            // 2. Callback-Methoden definieren
            Action<List<StandardWerkzeugZuweisung>, int> onToolsSaved = (updatedAssignments, newStationCount) =>
            {
                // Wenn der Benutzer im Untermenü auf 'Speichern' klickt:
                _pendingStandardToolChanges = updatedAssignments;
                _pendingAnzahlStationen = newStationCount;

                // Wir aktualisieren EditingMaschine sofort, damit der Benutzer die Stationsanzahl in der Hauptansicht sieht.
                EditingMaschine!.AnzahlStationen = newStationCount;
            };

            Action onToolsCanceled = () =>
            {
                // Wird hier nicht benötigt, da Änderungen in _pending... verwaltet werden.
            };

            // 3. Navigation mit Callbacks
            _mainViewModel.NavigateTo(new StandardToolsManagementViewModel(
                _mainViewModel,
                EditingMaschine!,
                onToolsSaved,
                onToolsCanceled));
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
                    // Löscht alle Standardwerkzeugzuweisungen kaskadierend
                    context.Maschinen.Remove(machineToDelete);
                    context.SaveChanges();
                }
            }

            // 2. Logik für BESTEHENDE Maschine: Änderungen verwerfen
            if (EditingMaschine != null && EditingMaschine.MaschineID != 0)
            {
                // Verwerfe die ausstehenden Standardwerkzeug-Änderungen
                _pendingStandardToolChanges = null;
                _pendingAnzahlStationen = null;

                // Setze die Stationsanzahl auf den ursprünglichen Wert zurück
                EditingMaschine.AnzahlStationen = _originalAnzahlStationen;
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