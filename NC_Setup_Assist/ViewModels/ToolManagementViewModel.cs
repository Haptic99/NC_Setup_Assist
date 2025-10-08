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

        // --- NEU: String-Property für die UI-Eingabe der Steigung ---
        [ObservableProperty]
        private string? _pitchInputString;

        [ObservableProperty]
        private string? _searchTerm;

        // --- NEU: Für den Auswahlmodus ---
        private readonly Action<Werkzeug>? _onToolSelectedCallback;
        [ObservableProperty]
        private bool _isSelectionMode;


        public ToolManagementViewModel()
        {
            FilteredWerkzeuge = new ObservableCollection<Werkzeug>();
            Kategorien = new ObservableCollection<WerkzeugKategorie>();
            Unterkategorien = new ObservableCollection<WerkzeugUnterkategorie>();

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadKategorien();
                LoadTools();
            }
        }

        public ToolManagementViewModel(Action<Werkzeug> onToolSelectedCallback) : this()
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
            var katsFromDb = context.WerkzeugKategorien.ToList();
            Kategorien.Clear();
            foreach (var kat in katsFromDb)
            {
                Kategorien.Add(kat);
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
                                   .ToList();
                foreach (var sub in subs)
                {
                    Unterkategorien.Add(sub);
                }
            }
        }

        partial void OnSelectedUnterkategorieChanged(WerkzeugUnterkategorie? value)
        {
            // 1. Sichtbarkeit der Steigung prüfen
            IsPitchRequired = (value?.Name == "Gewindedrehstahl Aussen" ||
                               value?.Name == "Gewindedrehstahl Innen" ||
                               value?.Name == "Gewindedrehstahl");

            if (EditingTool != null)
            {
                // In dieser Version wird EditingTool.Steigung nur beim Speichern gesetzt.
                // Hier nur die Unterkategorie setzen.
            }
        }
        #endregion

        #region Commands (Neu, Bearbeiten, Speichern, Abbrechen, Löschen)
        [RelayCommand]
        private void NewTool()
        {
            EditingTool = new Werkzeug();
            EditingTool.Steigung = null;
            PitchInputString = string.Empty; // NEU: String-Feld leeren
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsInEditMode = true;
            IsPitchRequired = false;
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

                // NEU: Steigung aus dem Modell in das String-Feld übertragen
                PitchInputString = EditingTool.Steigung?.ToString(CultureInfo.InvariantCulture);
                if (PitchInputString != null)
                {
                    // Ersetze den Punkt der Invariant-Kultur durch das Komma (für die deutsche Anzeige)
                    PitchInputString = PitchInputString.Replace('.', ',');
                }

                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == SelectedTool.Unterkategorie.WerkzeugKategorieID);
                SelectedUnterkategorie = SelectedTool.Unterkategorie;

                IsInEditMode = true;
            }
        }

        [RelayCommand]
        private void SaveTool()
        {
            if (EditingTool == null || SelectedUnterkategorie == null)
            {
                MessageBox.Show("Bitte wählen Sie eine Kategorie und einen Typ aus.", "Fehlende Eingabe");
                return;
            }

            // --- NEUE ROBUSTE VALIDIERUNG DER STEIGUNG ---

            double? finalPitch = null;
            string rawInput = PitchInputString?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(rawInput))
            {
                // 1. Ersetze Komma durch Punkt, um das Parsen mit InvariantCulture (Punkt als Dezimaltrenner) zu ermöglichen
                string normalizedInput = rawInput.Replace(',', '.');

                // 2. TryParse mit InvariantCulture
                if (double.TryParse(normalizedInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsedValue))
                {
                    finalPitch = parsedValue;
                }
                else
                {
                    // Parsing fehlgeschlagen: Der Wert ist keine Zahl, kein Komma/Punkt, oder es gibt mehrere Separatoren
                    MessageBox.Show("Die eingegebene Steigung ist keine gültige Dezimalzahl (erlaubt sind nur Zahlen, Komma oder Punkt).", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // 3. Prüfung, ob die Steigung zwingend erforderlich ist
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
                    toolToUpdate.Steigung = EditingTool.Steigung; // Steigung aktualisieren
                    toolToUpdate.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                }
            }

            context.SaveChanges();
            LoadTools();
            IsInEditMode = false;
            EditingTool = null;
            SelectedTool = null;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsPitchRequired = false;
            PitchInputString = string.Empty; // NEU: String-Feld zurücksetzen
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingTool = null;
            IsInEditMode = false;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsPitchRequired = false;
            PitchInputString = string.Empty; // NEU: String-Feld zurücksetzen
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
        #endregion
    }
}