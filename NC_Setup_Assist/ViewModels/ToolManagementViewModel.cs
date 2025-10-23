// NC_Setup_Assist/ViewModels/ToolManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NC_Setup_Assist.ViewModels
{
    public partial class ToolManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        // Listen
        private List<Werkzeug> _allTools = new List<Werkzeug>();
        public ObservableCollection<Werkzeug> FilteredWerkzeuge { get; private set; }
        public ObservableCollection<WerkzeugKategorie> Kategorien { get; private set; }
        public ObservableCollection<WerkzeugUnterkategorie> Unterkategorien { get; private set; }

        // Zustände und Auswahlen
        [ObservableProperty]
        private Werkzeug? _selectedTool;

        [ObservableProperty]
        private Werkzeug? _editingTool;

        [ObservableProperty]
        private bool _isInEditMode;

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorie;

        [ObservableProperty]
        private WerkzeugUnterkategorie? _selectedUnterkategorie;

        [ObservableProperty]
        private string? _searchTerm;

        // Workflow-Steuerung
        [ObservableProperty]
        private string? _toolName;

        [ObservableProperty]
        private bool _isUnterkategorieEnabled;

        [ObservableProperty]
        private bool _isToolDetailsEnabled;

        // --- DYNAMISCHE EIGENSCHAFTEN ---
        [ObservableProperty]
        private bool _isPitchRequired; // Gesteuert durch Unterkategorie

        [ObservableProperty]
        private string? _pitchInputString; // Wert für Steigung

        [ObservableProperty]
        private bool _isPlattenwinkelRequired; // NEU: Gesteuert durch Unterkategorie

        [ObservableProperty]
        private string? _plattenwinkelInputString; // NEU: Wert für Winkel

        // Auswahlmodus
        private readonly Action<Werkzeug>? _onToolSelectedCallback;
        [ObservableProperty]
        private bool _isSelectionMode;


        public ToolManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            FilteredWerkzeuge = new ObservableCollection<Werkzeug>();
            Kategorien = new ObservableCollection<WerkzeugKategorie>();
            Unterkategorien = new ObservableCollection<WerkzeugUnterkategorie>();

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadKategorien();
                LoadTools();
            }
        }

        public ToolManagementViewModel(MainViewModel mainViewModel, Action<Werkzeug> onToolSelectedCallback) : this(mainViewModel)
        {
            _onToolSelectedCallback = onToolSelectedCallback;
            IsSelectionMode = true;
        }

        #region Lade- und Filter-Logik

        private void LoadTools()
        {
            using var context = new NcSetupContext();
            _allTools = context.Werkzeuge
                               .Include(w => w.Unterkategorie)
                               .ThenInclude(u => u.Kategorie) // Wichtig: Kategorie muss mitgeladen werden
                               .ToList();
            ApplyFilter();
        }

        private void LoadKategorien()
        {
            using var context = new NcSetupContext();
            var katsFromDb = context.WerkzeugKategorien.OrderBy(k => k.Name).ToList();
            Kategorien.Clear();
            foreach (var kat in katsFromDb)
            {
                Kategorien.Add(kat);
            }
        }

        private void RefreshKategorienData()
        {
            var oldKatId = SelectedKategorie?.WerkzeugKategorieID;
            var oldUnterKatId = SelectedUnterkategorie?.WerkzeugUnterkategorieID;

            LoadKategorien();

            if (oldKatId.HasValue)
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == oldKatId.Value);
            }

            if (SelectedKategorie != null && oldUnterKatId.HasValue)
            {
                // OnSelectedKategorieChanged lädt die Unterkategorien neu
                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u => u.WerkzeugUnterkategorieID == oldUnterKatId.Value);
            }
        }

        private void ApplyFilter()
        {
            // (Unverändert)
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredWerkzeuge.Clear();
                foreach (var tool in _allTools)
                {
                    FilteredWerkzeuge.Add(tool);
                }
            }
            else
            {
                var filtered = _allTools.Where(w =>
                    (w.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Unterkategorie?.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Beschreibung?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();

                FilteredWerkzeuge.Clear();
                foreach (var tool in filtered)
                {
                    FilteredWerkzeuge.Add(tool);
                }
            }
        }

        partial void OnSearchTermChanged(string? value) => ApplyFilter();

        #endregion

        #region Kaskadierende Dropdown-Logik

        partial void OnSelectedToolChanged(Werkzeug? value)
        {
            // (Unverändert)
            if (value?.Unterkategorie?.Kategorie != null)
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == value.Unterkategorie.Kategorie.WerkzeugKategorieID);
                SelectedUnterkategorie = value.Unterkategorie;
            }
            else
            {
                SelectedKategorie = null;
                SelectedUnterkategorie = null;
            }
        }

        partial void OnSelectedKategorieChanged(WerkzeugKategorie? value)
        {
            // (Unverändert, lädt Unterkategorien)
            using var context = new NcSetupContext();
            Unterkategorien.Clear();
            SelectedUnterkategorie = null;
            if (value != null)
            {
                var subs = context.WerkzeugUnterkategorien
                                   .Where(s => s.WerkzeugKategorieID == value.WerkzeugKategorieID)
                                   .OrderBy(s => s.Name)
                                   .ToList();
                foreach (var sub in subs)
                {
                    Unterkategorien.Add(sub);
                }
            }

            IsUnterkategorieEnabled = value != null;
            if (!IsUnterkategorieEnabled)
            {
                IsToolDetailsEnabled = false;
            }
        }

        partial void OnSelectedUnterkategorieChanged(WerkzeugUnterkategorie? value)
        {
            // (Unverändert)
            IsPitchRequired = (value?.BenötigtSteigung == true);
            IsPlattenwinkelRequired = (value?.BenötigtPlattenwinkel == true);

            if (value != null)
            {
                IsToolDetailsEnabled = true;
                if (EditingTool?.WerkzeugID == 0)
                {
                    PitchInputString = string.Empty;
                    PlattenwinkelInputString = string.Empty;
                }
                UpdateToolName();
            }
            else
            {
                IsToolDetailsEnabled = false;
                ToolName = string.Empty;
            }
        }

        partial void OnPitchInputStringChanged(string? value)
        {
            UpdateToolName();
        }

        partial void OnPlattenwinkelInputStringChanged(string? value)
        {
            UpdateToolName();
        }

        private void UpdateToolName()
        {
            // (Unverändert)
            if (EditingTool == null || SelectedUnterkategorie == null)
            {
                return;
            }

            string baseName = SelectedUnterkategorie.Name;
            var sb = new StringBuilder(baseName);

            if (IsPitchRequired)
            {
                string pitchDisplay = (PitchInputString ?? "").Trim().Replace(',', '.');
                if (!string.IsNullOrEmpty(pitchDisplay))
                {
                    sb.Append($" P={pitchDisplay}");
                }
            }

            if (IsPlattenwinkelRequired)
            {
                string winkelDisplay = (PlattenwinkelInputString ?? "").Trim().Replace(',', '.');
                if (!string.IsNullOrEmpty(winkelDisplay))
                {
                    sb.Append($" {winkelDisplay}°");
                }
            }

            ToolName = sb.ToString();
        }

        #endregion

        #region Commands (Neu, Bearbeiten, Speichern, Abbrechen, Löschen)
        [RelayCommand]
        private void NewTool()
        {
            // (Unverändert)
            EditingTool = new Werkzeug
            {
                Steigung = null,
                Plattenwinkel = null
            };

            ToolName = string.Empty;
            PitchInputString = string.Empty;
            PlattenwinkelInputString = string.Empty;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsInEditMode = true;
            IsPitchRequired = false;
            IsPlattenwinkelRequired = false;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;
        }

        [RelayCommand]
        private void EditTool()
        {
            if (SelectedTool == null) return;

            if (IsSelectionMode)
            {
                _onToolSelectedCallback?.Invoke(SelectedTool);
            }
            else
            {
                EditingTool = new Werkzeug
                {
                    WerkzeugID = SelectedTool.WerkzeugID,
                    Name = SelectedTool.Name,
                    Beschreibung = SelectedTool.Beschreibung,
                    Steigung = SelectedTool.Steigung,
                    Plattenwinkel = SelectedTool.Plattenwinkel,
                    Unterkategorie = SelectedTool.Unterkategorie,
                    WerkzeugUnterkategorieID = SelectedTool.WerkzeugUnterkategorieID
                };

                ToolName = EditingTool.Name;

                PitchInputString = EditingTool.Steigung?.ToString("G", CultureInfo.CurrentCulture);
                PlattenwinkelInputString = EditingTool.Plattenwinkel?.ToString("G", CultureInfo.CurrentCulture);

                // --- KORREKTUR (Request 2) ---
                // Hier wurde fälschlicherweise 'SelectedTool.Unterkategorie.WerkzeugKategorieID' verwendet.
                // Richtig ist 'SelectedTool.Unterkategorie.Kategorie.WerkzeugKategorieID'.
                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == SelectedTool.Unterkategorie.Kategorie.WerkzeugKategorieID);

                // Diese Zuweisung funktioniert jetzt, da 'SelectedKategorie' (oben) 'OnSelectedKategorieChanged' auslöst
                // und die 'Unterkategorien'-Liste korrekt füllt, BEVOR diese Zeile ausgeführt wird.
                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u => u.WerkzeugUnterkategorieID == SelectedTool.WerkzeugUnterkategorieID);

                IsInEditMode = true;
                IsUnterkategorieEnabled = true;
                IsToolDetailsEnabled = true;
            }
        }

        // Helper-Funktion zum Parsen
        private bool ParseNullableDouble(string input, bool isRequired, string fieldName, out double? result)
        {
            // (Unverändert)
            result = null;
            string rawInput = input?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(rawInput))
            {
                if (isRequired)
                {
                    MessageBox.Show($"Für diesen Werkzeugtyp ist die Angabe '{fieldName}' erforderlich.", "Fehlende Eingabe");
                    return false;
                }
                return true;
            }

            string normalizedInput = rawInput.Replace(',', '.');
            if (double.TryParse(normalizedInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsedValue))
            {
                result = parsedValue;
                return true;
            }
            else
            {
                MessageBox.Show($"Der Wert für '{fieldName}' ist keine gültige Zahl.", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        [RelayCommand]
        private void SaveTool()
        {
            // (Unverändert)
            if (EditingTool == null || SelectedUnterkategorie == null || string.IsNullOrWhiteSpace(ToolName))
            {
                MessageBox.Show("Bitte wählen Sie eine Kategorie, einen Typ aus und geben Sie einen Namen ein.", "Fehlende Eingabe");
                return;
            }

            double? finalPitch;
            if (!ParseNullableDouble(PitchInputString, IsPitchRequired, "Steigung", out finalPitch))
            {
                return;
            }

            double? finalPlattenwinkel;
            if (!ParseNullableDouble(PlattenwinkelInputString, IsPlattenwinkelRequired, "Plattenwinkel", out finalPlattenwinkel))
            {
                return;
            }

            EditingTool.Steigung = finalPitch;
            EditingTool.Plattenwinkel = finalPlattenwinkel;
            EditingTool.Name = ToolName;

            using var context = new NcSetupContext();

            if (EditingTool.WerkzeugID == 0)
            {
                EditingTool.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                context.WerkzeugUnterkategorien.Attach(SelectedUnterkategorie);
                context.Werkzeuge.Add(EditingTool);
            }
            else
            {
                var toolToUpdate = context.Werkzeuge.Find(EditingTool.WerkzeugID);
                if (toolToUpdate != null)
                {
                    toolToUpdate.Name = EditingTool.Name;
                    toolToUpdate.Beschreibung = EditingTool.Beschreibung;
                    toolToUpdate.Steigung = EditingTool.Steigung;
                    toolToUpdate.Plattenwinkel = EditingTool.Plattenwinkel;
                    toolToUpdate.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                }
            }

            context.SaveChanges();
            LoadTools();
            IsInEditMode = false;
            EditingTool = null;
            ToolName = string.Empty;
            SelectedTool = null;
            Cancel();
        }

        [RelayCommand]
        private void Cancel()
        {
            // (Unverändert)
            EditingTool = null;
            IsInEditMode = false;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;

            IsPitchRequired = false;
            IsPlattenwinkelRequired = false;
            PitchInputString = string.Empty;
            PlattenwinkelInputString = string.Empty;
            ToolName = string.Empty;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;
        }

        [RelayCommand]
        private void DeleteTool()
        {
            // (Unverändert)
            if (SelectedTool == null) return;

            var result = MessageBox.Show($"Möchten Sie das Werkzeug '{SelectedTool.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var context = new NcSetupContext();
                var toolToDelete = context.Werkzeuge.Find(SelectedTool.WerkzeugID);
                if (toolToDelete != null)
                {
                    context.Werkzeuge.Remove(toolToDelete);
                    context.SaveChanges();
                }
                LoadTools();
            }
        }

        [RelayCommand]
        private void NavigateToKategorieManagement()
        {
            // (Unverändert)
            _mainViewModel.NavigateTo(new KategorieManagementViewModel(RefreshKategorienData));
        }

        [RelayCommand]
        private void NavigateToUnterkategorieManagement()
        {
            // (Unverändert)
            _mainViewModel.NavigateTo(new UnterkategorieManagementViewModel(RefreshKategorienData));
        }

        #endregion
    }
}