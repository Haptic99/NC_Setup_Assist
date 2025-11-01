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
using NC_Setup_Assist.Services; // <-- NEU

namespace NC_Setup_Assist.ViewModels
{
    public partial class MachineManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private Maschine? _machineToDeleteOnCancel;

        // Hält die UNSICHEREN Änderungen der Standardwerkzeuge
        private List<StandardWerkzeugZuweisung>? _pendingStandardToolChanges;
        private int _originalAnzahlStationen;
        private int? _pendingAnzahlStationen;

        // --- NEU: Listen und SearchTerm ---
        private List<Maschine> _allMaschinen = new(); // Hält ungefilterte Daten

        public ObservableCollection<Maschine> FilteredMaschinen { get; } = new(); // Sichtbare Liste

        public ObservableCollection<Hersteller> Hersteller { get; } = new();
        public ObservableCollection<Standort> Standorte { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditMachineCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteMachineCommand))]
        private Maschine? _selectedMaschine;

        [ObservableProperty]
        private Maschine? _editingMaschine;

        [ObservableProperty]
        private bool _isInEditMode;

        [ObservableProperty] // NEU
        private string? _searchTerm;
        // ------------------------------------

        public MachineManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadData();
        }

        // NEU: Reagiert auf Änderungen im Suchfeld
        partial void OnSearchTermChanged(string? value) => ApplyFilter();

        private void LoadData()
        {
            // --- NEU: try-catch für Ladeoperationen ---
            try
            {
                _allMaschinen.Clear();
                FilteredMaschinen.Clear();

                using var context = new NcSetupContext();
                var maschinenFromDb = context.Maschinen
                                             .Include(m => m.Hersteller)
                                             .Include(m => m.ZugehoerigerStandort)
                                             .ToList();

                _allMaschinen.AddRange(maschinenFromDb);

                // Hersteller und Standorte laden (unverändert)
                Hersteller.Clear();
                var herstellerFromDb = context.Hersteller.OrderBy(h => h.Name).ToList();
                foreach (var herst in herstellerFromDb)
                {
                    Hersteller.Add(herst);
                }

                Standorte.Clear();
                var standorteFromDb = context.Standorte.ToList();
                foreach (var standort in standorteFromDb)
                {
                    Standorte.Add(standort);
                }

                ApplyFilter(); // Filter anwenden
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Maschinendaten");
                MessageBox.Show($"Ein Fehler ist beim Laden der Daten aufgetreten:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // --- ENDE NEU ---
        }

        private void ApplyFilter()
        {
            FilteredMaschinen.Clear();
            var filter = this.SearchTerm?.Trim().ToLower() ?? "";

            var filteredList = _allMaschinen.Where(m =>
                string.IsNullOrWhiteSpace(filter) ||
                m.Name.ToLower().Contains(filter) ||
                (m.Seriennummer?.ToLower().Contains(filter) ?? false) ||
                (m.Hersteller?.Name.ToLower().Contains(filter) ?? false) ||
                (m.ZugehoerigerStandort?.Name.ToLower().Contains(filter) ?? false)
            ).ToList();

            foreach (var maschine in filteredList)
            {
                FilteredMaschinen.Add(maschine);
            }
        }

        private void RefreshDataAndEditingState()
        {
            // IDs der aktuellen Auswahl merken (falls vorhanden)
            int? editingMachineId = EditingMaschine?.MaschineID;
            var selectedHerstellerId = EditingMaschine?.Hersteller?.HerstellerID;
            var selectedStandortId = EditingMaschine?.ZugehoerigerStandort?.StandortID;

            // Alle Daten neu aus der DB laden
            LoadData(); // Ruft jetzt LoadData/ApplyFilter auf

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
                    var refreshedMachineInList = _allMaschinen.FirstOrDefault(m => m.MaschineID == editingMachineId.Value);
                    if (refreshedMachineInList != null)
                    {
                        EditingMaschine.AnzahlStationen = refreshedMachineInList.AnzahlStationen;
                    }
                }
            }
        }


        private bool CanExecuteMachineCommand()
        {
            return SelectedMaschine != null;
        }

        [RelayCommand]
        private void NewMachine()
        {
            // ... (unveränderte Logik)
            EditingMaschine = new Maschine();
            IsInEditMode = true;
            _machineToDeleteOnCancel = null;

            _pendingStandardToolChanges = null;
            _pendingAnzahlStationen = null;
            _originalAnzahlStationen = 0;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteMachineCommand))]
        private void EditMachine()
        {
            // ... (unveränderte Logik)
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

            // --- NEU: try-catch um die gesamte Datenbankoperation ---
            try
            {
                using var context = new NcSetupContext();
                Maschine machineToSave;
                bool isNewMachine = EditingMaschine.MaschineID == 0;

                if (isNewMachine)
                {
                    machineToSave = new Maschine
                    {
                        Name = EditingMaschine.Name,
                        Seriennummer = EditingMaschine.Seriennummer,
                        AnzahlStationen = _pendingAnzahlStationen ?? 0,
                        HerstellerID = EditingMaschine.Hersteller.HerstellerID,
                        StandortID = EditingMaschine.ZugehoerigerStandort.StandortID
                    };
                    context.Maschinen.Add(machineToSave);
                    context.SaveChanges(); // <-- Riskanter Aufruf

                    EditingMaschine.MaschineID = machineToSave.MaschineID;
                }
                else
                {
                    machineToSave = context.Maschinen.Find(EditingMaschine.MaschineID)!;

                    if (machineToSave == null) return; // Sollte nicht passieren, aber sicher ist sicher

                    machineToSave.Name = EditingMaschine.Name;
                    machineToSave.Seriennummer = EditingMaschine.Seriennummer;
                    machineToSave.HerstellerID = EditingMaschine.Hersteller.HerstellerID;
                    machineToSave.StandortID = EditingMaschine.ZugehoerigerStandort.StandortID;

                    machineToSave.AnzahlStationen = _pendingAnzahlStationen ?? EditingMaschine.AnzahlStationen;
                }

                EditingMaschine.AnzahlStationen = machineToSave.AnzahlStationen;

                if (_pendingStandardToolChanges != null)
                {
                    var existingAssignments = context.StandardWerkzeugZuweisungen
                        .Where(z => z.MaschineID == machineToSave.MaschineID);
                    context.StandardWerkzeugZuweisungen.RemoveRange(existingAssignments);

                    var toolIds = _pendingStandardToolChanges.Select(a => a.WerkzeugID).Distinct();

                    var minimalKategorieStub = new WerkzeugKategorie { WerkzeugKategorieID = 0, Name = "Stub" };
                    var minimalUnterkategorieStub = new WerkzeugUnterkategorie
                    {
                        WerkzeugUnterkategorieID = 0,
                        Name = "Stub",
                        Kategorie = minimalKategorieStub
                    };

                    context.Entry(minimalKategorieStub).State = EntityState.Detached;
                    context.Entry(minimalUnterkategorieStub).State = EntityState.Detached;

                    foreach (var toolId in toolIds)
                    {
                        var werkzeugStub = new Werkzeug
                        {
                            WerkzeugID = toolId,
                            Name = "PLACEHOLDER",
                            WerkzeugUnterkategorieID = minimalUnterkategorieStub.WerkzeugUnterkategorieID,
                            Unterkategorie = minimalUnterkategorieStub
                        };

                        context.Werkzeuge.Attach(werkzeugStub);
                        context.Entry(werkzeugStub).State = EntityState.Unchanged;
                    }

                    context.StandardWerkzeugZuweisungen.AddRange(_pendingStandardToolChanges);
                }

                if (context.ChangeTracker.HasChanges())
                {
                    context.SaveChanges(); // <-- Riskanter Aufruf
                }

                // Diese Logik wird nur bei Erfolg ausgeführt:
                _pendingStandardToolChanges = null;
                _pendingAnzahlStationen = null;
                _originalAnzahlStationen = EditingMaschine!.AnzahlStationen;

                _machineToDeleteOnCancel = null;

                IsInEditMode = false;
                EditingMaschine = null;
                LoadData();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Speichern der Maschine");
                MessageBox.Show($"Die Maschine konnte nicht gespeichert werden:\n{ex.Message}\n\nEin Fehlerbericht wurde in 'error_log.txt' gespeichert.", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // --- ENDE NEU ---
        }

        [RelayCommand(CanExecute = nameof(CanExecuteMachineCommand))]
        private void DeleteMachine()
        {
            if (SelectedMaschine == null) return;

            var result = MessageBox.Show($"Möchten Sie die Maschine '{SelectedMaschine.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // --- NEU: try-catch für Löschoperation ---
                try
                {
                    using var context = new NcSetupContext();
                    var machineToDelete = context.Maschinen.Find(SelectedMaschine.MaschineID);
                    if (machineToDelete != null)
                    {
                        context.Maschinen.Remove(machineToDelete);
                        context.SaveChanges(); // <-- Riskanter Aufruf
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    LoggingService.LogException(ex, "Fehler beim Löschen der Maschine");
                    MessageBox.Show($"Die Maschine konnte nicht gelöscht werden:\n{ex.Message}\n\nDies kann passieren, wenn die Maschine noch in Projekten verwendet wird.", "Löschfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // --- ENDE NEU ---
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

            // --- NEU: try-catch für das temporäre Speichern ---
            try
            {
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
                    context.SaveChanges(); // <-- Riskanter Aufruf

                    EditingMaschine.Hersteller = tempHersteller;
                    EditingMaschine.ZugehoerigerStandort = tempStandort;

                    _machineToDeleteOnCancel = EditingMaschine;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim temporären Speichern der Maschine für Standardwerkzeuge");
                MessageBox.Show($"Ein Fehler ist aufgetreten:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Navigation nicht ausführen, wenn Speichern fehlschlägt
            }
            // --- ENDE NEU ---

            Action<List<StandardWerkzeugZuweisung>, int> onToolsSaved = (updatedAssignments, newStationCount) =>
            {
                _pendingStandardToolChanges = updatedAssignments;
                _pendingAnzahlStationen = newStationCount;
                EditingMaschine!.AnzahlStationen = newStationCount;
            };

            Action onToolsCanceled = () =>
            {
                // Leer
            };

            _mainViewModel.NavigateTo(new StandardToolsManagementViewModel(
                _mainViewModel,
                EditingMaschine!,
                onToolsSaved,
                onToolsCanceled,
                _pendingStandardToolChanges));
        }

        [RelayCommand]
        private void Cancel()
        {
            // --- NEU: try-catch für das Löschen der temporären Maschine ---
            try
            {
                if (_machineToDeleteOnCancel != null)
                {
                    using var context = new NcSetupContext();
                    var machineToDelete = context.Maschinen.Find(_machineToDeleteOnCancel.MaschineID);
                    if (machineToDelete != null)
                    {
                        context.Maschinen.Remove(machineToDelete);
                        context.SaveChanges(); // <-- Riskanter Aufruf
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Abbrechen/Löschen der temporären Maschine");
                // Hier ist eine MessageBox optional, da der Benutzer abbricht.
            }
            // --- ENDE NEU ---

            if (EditingMaschine != null && EditingMaschine.MaschineID != 0)
            {
                _pendingStandardToolChanges = null;
                _pendingAnzahlStationen = null;
                EditingMaschine.AnzahlStationen = _originalAnzahlStationen;
            }

            EditingMaschine = null;
            IsInEditMode = false;
            _machineToDeleteOnCancel = null;
            LoadData(); // LoadData hat bereits sein eigenes try-catch
        }

        [RelayCommand]
        private void AddHersteller()
        {
            // ... (unveränderte Logik)
            _mainViewModel.NavigateTo(new HerstellerManagementViewModel(RefreshDataAndEditingState));
        }
    }
}