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
using System.Globalization; // NEU: Für die robuste Parselogik
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NC_Setup_Assist.ViewModels
{
    public partial class ToolManagementViewModel : ViewModelBase
    {
        // --- NEU: MainViewModel für Navigation ---
        private readonly MainViewModel _mainViewModel;

        // --- Listen für die Daten ---
        private List<Werkzeug> _allTools = new List<Werkzeug>();
        public ObservableCollection<Werkzeug> FilteredWerkzeuge { get; private set; }
        public ObservableCollection<WerkzeugKategorie> Kategorien { get; private set; }
        public ObservableCollection<WerkzeugUnterkategorie> Unterkategorien { get; private set; }

        // --- Eigenschaften für Zustände und Auswahlen ---
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
        private bool _isPitchRequired;

        [ObservableProperty]
        private string? _pitchInputString;

        [ObservableProperty]
        private string? _searchTerm;

        // --- NEU: Eigenschaften zur Workflow-Steuerung ---

        [ObservableProperty]
        private string? _toolName; // Bindet an die Name-Textbox

        [ObservableProperty]
        private bool _isUnterkategorieEnabled; // Steuert das Unterkategorie-Dropdown

        [ObservableProperty]
        private bool _isToolDetailsEnabled; // Steuert Name, Steigung, Beschreibung

        // --- NEU: Für den Auswahlmodus ---
        private readonly Action<Werkzeug>? _onToolSelectedCallback;
        [ObservableProperty]
        private bool _isSelectionMode;


        // --- ANGEPASSTER KONSTRUKTOR (Nimmt jetzt MainViewModel) ---
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

        // --- ANGEPASSTER KONSTRUKTOR (Für Auswahlmodus) ---
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
                               .ThenInclude(u => u.Kategorie)
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

        // --- NEUE METHODE: Callback zum Aktualisieren der Dropdowns ---
        private void RefreshKategorienData()
        {
            // Selektionen merken
            var oldKatId = SelectedKategorie?.WerkzeugKategorieID;
            var oldUnterKatId = SelectedUnterkategorie?.WerkzeugUnterkategorieID;

            // Kategorien neu laden
            LoadKategorien();

            // Alte Auswahl wiederherstellen
            if (oldKatId.HasValue)
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == oldKatId.Value);
            }

            // Wenn die Hauptkategorie wiederhergestellt wurde, wurde OnSelectedKategorieChanged ausgelöst
            // und die Unterkategorien wurden neu geladen. Jetzt auch die Unterkategorie wiederherstellen.
            if (SelectedKategorie != null && oldUnterKatId.HasValue)
            {
                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u => u.WerkzeugUnterkategorieID == oldUnterKatId.Value);
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                // Wenn das Suchfeld leer ist, zeige alle Werkzeuge an
                FilteredWerkzeuge.Clear();
                foreach (var tool in _allTools)
                {
                    FilteredWerkzeuge.Add(tool);
                }
            }
            else
            {
                // Wenn Text im Suchfeld steht, filtere danach
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
            using var context = new NcSetupContext();
            Unterkategorien.Clear();
            SelectedUnterkategorie = null;
            if (value != null)
            {
                var subs = context.WerkzeugUnterkategorien
                                   .Where(s => s.WerkzeugKategorieID == value.WerkzeugKategorieID)
                                   .OrderBy(s => s.Name) // Sortieren
                                   .ToList();
                foreach (var sub in subs)
                {
                    Unterkategorien.Add(sub);
                }
            }

            // NEU: Workflow-Steuerung
            if (value != null)
            {
                IsUnterkategorieEnabled = true;
            }
            else
            {
                IsUnterkategorieEnabled = false;
                IsToolDetailsEnabled = false; // Wenn Kategorie zurückgesetzt wird, alles sperren
            }
        }

        partial void OnSelectedUnterkategorieChanged(WerkzeugUnterkategorie? value)
        {
            // 1. Sichtbarkeit der Steigung prüfen
            IsPitchRequired = (value?.Name == "Gewindedrehstahl Aussen" ||
                               value?.Name == "Gewindedrehstahl Innen");

            // NEU: Workflow-Steuerung
            if (value != null)
            {
                IsToolDetailsEnabled = true;
                UpdateToolName(); // Neuen Namen generieren
            }
            else
            {
                IsToolDetailsEnabled = false;
                ToolName = string.Empty;
            }

            if (EditingTool != null)
            {
                // In dieser Version wird EditingTool.Steigung nur beim Speichern gesetzt.
                // Hier nur die Unterkategorie setzen.
            }
        }

        // NEU: Reagiert auf Eingabe der Steigung
        partial void OnPitchInputStringChanged(string? value)
        {
            UpdateToolName();
        }

        // NEU: Methode zur Namensgenerierung
        private void UpdateToolName()
        {
            if (EditingTool == null || SelectedUnterkategorie == null)
            {
                return;
            }

            // Wenn es sich um einen Gewindestahl handelt
            if (IsPitchRequired)
            {
                // Ersetze Komma durch Punkt für die Anzeige
                string pitchDisplay = (PitchInputString ?? "").Replace(',', '.');

                ToolName = $"{SelectedUnterkategorie.Name} P= {pitchDisplay}";
            }
            else
            {
                // Für alle anderen Werkzeuge
                ToolName = SelectedUnterkategorie.Name;
            }
        }

        #endregion

        #region Commands (Neu, Bearbeiten, Speichern, Abbrechen, Löschen)
        [RelayCommand]
        private void NewTool()
        {
            EditingTool = new Werkzeug();
            EditingTool.Steigung = null;

            ToolName = string.Empty; // NEU
            PitchInputString = string.Empty;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsInEditMode = true;
            IsPitchRequired = false;

            // NEU: Workflow-Status setzen
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
                    Unterkategorie = SelectedTool.Unterkategorie,
                    WerkzeugUnterkategorieID = SelectedTool.WerkzeugUnterkategorieID
                };

                ToolName = EditingTool.Name; // NEU

                PitchInputString = EditingTool.Steigung?.ToString(CultureInfo.InvariantCulture);
                if (PitchInputString != null)
                {
                    PitchInputString = PitchInputString.Replace('.', ',');
                }

                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == SelectedTool.Unterkategorie.WerkzeugKategorieID);
                SelectedUnterkategorie = SelectedTool.Unterkategorie;

                IsInEditMode = true;

                // NEU: Beim Bearbeiten alles aktivieren
                IsUnterkategorieEnabled = true;
                IsToolDetailsEnabled = true;
            }
        }

        [RelayCommand]
        private void SaveTool()
        {
            if (EditingTool == null || SelectedUnterkategorie == null || string.IsNullOrWhiteSpace(ToolName)) // NEU: ToolName geprüft
            {
                MessageBox.Show("Bitte wählen Sie eine Kategorie, einen Typ aus und geben Sie einen Namen ein.", "Fehlende Eingabe");
                return;
            }

            // --- NEUE ROBUSTE VALIDIERUNG DER STEIGUNG ---
            double? finalPitch = null;
            string rawInput = PitchInputString?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(rawInput))
            {
                string normalizedInput = rawInput.Replace(',', '.');

                if (double.TryParse(normalizedInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsedValue))
                {
                    finalPitch = parsedValue;
                }
                else
                {
                    MessageBox.Show("Die eingegebene Steigung ist keine gültige Dezimalzahl (erlaubt sind nur Zahlen, Komma oder Punkt).", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (IsPitchRequired)
            {
                if (finalPitch == null || finalPitch.Value <= 0)
                {
                    MessageBox.Show("Für Gewindedrehstähle muss die Steigung (Pitch) als Wert größer 0 angegeben werden.", "Fehlende Steigung");
                    return;
                }
            }

            // 4. Den finalen, validierten Wert in das Modell schreiben
            EditingTool.Steigung = finalPitch;
            EditingTool.Name = ToolName; // NEU: ToolName explizit zuweisen

            // ---------------------------------------------

            using var context = new NcSetupContext();

            if (EditingTool.WerkzeugID == 0)
            {
                EditingTool.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                EditingTool.Unterkategorie = SelectedUnterkategorie;
                context.WerkzeugUnterkategorien.Attach(EditingTool.Unterkategorie);

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
                    toolToUpdate.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                }
            }

            context.SaveChanges();
            LoadTools();
            IsInEditMode = false;
            EditingTool = null;
            ToolName = string.Empty; // NEU
            SelectedTool = null;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsPitchRequired = false;
            PitchInputString = string.Empty;
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingTool = null;
            IsInEditMode = false;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsPitchRequired = false;
            PitchInputString = string.Empty;

            // NEU: Workflow-Status zurücksetzen
            ToolName = string.Empty;
            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;
        }

        [RelayCommand]
        private void DeleteTool()
        {
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

        // --- NEUE COMMANDS FÜR KATEGORIE-MANAGEMENT ---

        [RelayCommand]
        private void AddKategorie()
        {
            // Öffnet die neue Ansicht und übergibt die Refresh-Methode als Callback
            _mainViewModel.NavigateTo(new WerkzeugKategorieManagementViewModel(RefreshKategorienData));
        }

        [RelayCommand]
        private void AddUnterkategorie()
        {
            // Öffnet die neue Ansicht und übergibt die Refresh-Methode als Callback
            _mainViewModel.NavigateTo(new WerkzeugKategorieManagementViewModel(RefreshKategorienData));
        }

        #endregion
    }
}