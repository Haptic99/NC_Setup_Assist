// NC_Setup_Assist/ViewModels/ToolManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services; // <-- NEU
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

        // ... (Rest der Properties bleibt gleich) ...
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

        [ObservableProperty]
        private string? _toolName;

        [ObservableProperty]
        private bool _isUnterkategorieEnabled;

        [ObservableProperty]
        private bool _isToolDetailsEnabled;

        [ObservableProperty]
        private bool _isRadiusRequired;

        [ObservableProperty]
        private string? _radiusInputString;

        [ObservableProperty]
        private bool _isPitchRequired;

        [ObservableProperty]
        private string? _pitchInputString;

        [ObservableProperty]
        private bool _isSpitzenwinkelRequired;

        [ObservableProperty]
        private string? _spitzenwinkelInputString;

        [ObservableProperty]
        private bool _isDurchmesserRequired;

        [ObservableProperty]
        private string? _durchmesserInputString;

        [ObservableProperty]
        private bool _isBreiteRequired;

        [ObservableProperty]
        private string? _breiteInputString;

        [ObservableProperty]
        private bool _isMaxStechtiefeRequired;

        [ObservableProperty]
        private string? _maxStechtiefeInputString;

        private readonly Action<Werkzeug>? _onToolSelectedCallback;
        [ObservableProperty]
        private bool _isSelectionMode;

        private readonly string? _initialFavoritKategorie;
        private readonly string? _initialFavoritUnterkategorie;


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

        // ... (Konstruktor für Auswahlmodus bleibt gleich) ...
        public ToolManagementViewModel(MainViewModel mainViewModel,
                                       Action<Werkzeug> onToolSelectedCallback,
                                       string? favoritKategorie = null,
                                       string? favoritUnterkategorie = null) : this(mainViewModel)
        {
            _onToolSelectedCallback = onToolSelectedCallback;
            IsSelectionMode = true;
            _initialFavoritKategorie = favoritKategorie;
            _initialFavoritUnterkategorie = favoritUnterkategorie;
            ApplyFavoritFilter(_initialFavoritKategorie, _initialFavoritUnterkategorie);
        }


        #region Lade- und Filter-Logik

        // ... (ApplyFavoritFilter bleibt gleich) ...
        private void ApplyFavoritFilter(string? favoritKategorie, string? favoritUnterkategorie)
        {
            if (!string.IsNullOrEmpty(favoritUnterkategorie) && !string.IsNullOrEmpty(favoritKategorie))
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k =>
                    k.Name.Equals(favoritKategorie, StringComparison.OrdinalIgnoreCase));
                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u =>
                    u.Name.Equals(favoritUnterkategorie, StringComparison.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrEmpty(favoritKategorie))
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k =>
                    k.Name.Equals(favoritKategorie, StringComparison.OrdinalIgnoreCase));
                SelectedUnterkategorie = null;
            }
            else
            {
                SelectedKategorie = null;
                SelectedUnterkategorie = null;
            }
        }


        private void LoadTools()
        {
            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();
                _allTools = context.Werkzeuge
                                   .Include(w => w.Unterkategorie)
                                   .ThenInclude(u => u.Kategorie) // Wichtig: Kategorie muss mitgeladen werden
                                   .ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Werkzeuge");
                MessageBox.Show($"Fehler beim Laden der Werkzeuge:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadKategorien()
        {
            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();
                var katsFromDb = context.WerkzeugKategorien.OrderBy(k => k.Name).ToList();
                Kategorien.Clear();
                foreach (var kat in katsFromDb)
                {
                    Kategorien.Add(kat);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Werkzeugkategorien");
                MessageBox.Show($"Fehler beim Laden der Kategorien:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshKategorienData()
        {
            var oldKatId = SelectedKategorie?.WerkzeugKategorieID;
            var oldUnterKatId = SelectedUnterkategorie?.WerkzeugUnterkategorieID;

            LoadKategorien(); // Diese Methode hat jetzt ein try-catch

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

        // ... (ApplyFilter bleibt gleich) ...
        private void ApplyFilter()
        {
            IEnumerable<Werkzeug> tempFiltered = _allTools;
            var currentKategorie = SelectedKategorie;
            var currentUnterkategorie = SelectedUnterkategorie;
            var currentSearchTerm = SearchTerm;

            if (currentKategorie != null)
            {
                tempFiltered = tempFiltered.Where(w => w.Unterkategorie?.Kategorie?.WerkzeugKategorieID == currentKategorie.WerkzeugKategorieID);
            }
            if (currentUnterkategorie != null)
            {
                tempFiltered = tempFiltered.Where(w => w.WerkzeugUnterkategorieID == currentUnterkategorie.WerkzeugUnterkategorieID);
            }
            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                tempFiltered = tempFiltered.Where(w =>
                    (w.Name?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Unterkategorie?.Name?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Beschreibung?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            FilteredWerkzeuge.Clear();
            foreach (var tool in tempFiltered.ToList())
            {
                FilteredWerkzeuge.Add(tool);
            }
        }


        partial void OnSearchTermChanged(string? value) => ApplyFilter();

        #endregion

        #region Kaskadierende Dropdown-Logik

        partial void OnSelectedToolChanged(Werkzeug? value)
        {
            // (Rumpf bleibt leer)
        }

        partial void OnSelectedKategorieChanged(WerkzeugKategorie? value)
        {
            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();
                Unterkategorien.Clear();

                // Nur zurücksetzen, wenn die Änderung *nicht* Teil des Setzens des Unterkategorie-Filters ist
                if (SelectedUnterkategorie != null && SelectedUnterkategorie.WerkzeugKategorieID != value?.WerkzeugKategorieID)
                {
                    SelectedUnterkategorie = null; // WICHTIG: Unterkategorie zurücksetzen
                }

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

                ApplyFilter();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Unterkategorien");
                MessageBox.Show($"Fehler beim Laden der Unterkategorien:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ... (Rest der Region 'Kaskadierende Dropdown-Logik' bleibt unverändert) ...
        partial void OnSelectedUnterkategorieChanged(WerkzeugUnterkategorie? value)
        {
            IsRadiusRequired = (value?.BenötigtRadius == true);
            IsPitchRequired = (value?.BenötigtSteigung == true);
            IsSpitzenwinkelRequired = (value?.BenötigtSpitzenwinkel == true);
            IsDurchmesserRequired = (value?.BenötigtDurchmesser == true);
            IsBreiteRequired = (value?.BenötigtBreite == true);
            IsMaxStechtiefeRequired = (value?.BenötigtMaxStechtiefe == true);

            if (value != null)
            {
                IsToolDetailsEnabled = true;
                if (EditingTool?.WerkzeugID == 0)
                {
                    RadiusInputString = string.Empty;
                    PitchInputString = string.Empty;
                    SpitzenwinkelInputString = string.Empty;
                    DurchmesserInputString = string.Empty;
                    BreiteInputString = string.Empty;
                    MaxStechtiefeInputString = string.Empty;
                }
                UpdateToolName();
            }
            else
            {
                IsToolDetailsEnabled = false;
                ToolName = string.Empty;
            }

            ApplyFilter();
        }

        partial void OnRadiusInputStringChanged(string? value) => UpdateToolName();
        partial void OnPitchInputStringChanged(string? value) => UpdateToolName();
        partial void OnSpitzenwinkelInputStringChanged(string? value) => UpdateToolName();
        partial void OnDurchmesserInputStringChanged(string? value) => UpdateToolName();
        partial void OnBreiteInputStringChanged(string? value) => UpdateToolName();
        partial void OnMaxStechtiefeInputStringChanged(string? value) => UpdateToolName();

        private void UpdateToolName()
        {
            if (EditingTool == null || SelectedUnterkategorie == null)
            {
                return;
            }

            if (EditingTool.WerkzeugID == 0 || (EditingTool.WerkzeugUnterkategorieID != SelectedUnterkategorie.WerkzeugUnterkategorieID))
            {
                string baseName = SelectedUnterkategorie.Name;
                var sb = new StringBuilder(baseName);

                if (IsDurchmesserRequired)
                {
                    string display = (DurchmesserInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" D={display}");
                    }
                }
                if (IsRadiusRequired)
                {
                    string display = (RadiusInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" R={display}");
                    }
                }
                if (IsBreiteRequired)
                {
                    string display = (BreiteInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" B={display}");
                    }
                }
                if (IsPitchRequired)
                {
                    string display = (PitchInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" P={display}");
                    }
                }
                if (IsSpitzenwinkelRequired)
                {
                    string display = (SpitzenwinkelInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" {display}°");
                    }
                }
                if (IsMaxStechtiefeRequired)
                {
                    string display = (MaxStechtiefeInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(display))
                    {
                        sb.Append($" Tmax={display}");
                    }
                }
                ToolName = sb.ToString();
            }
        }

        #endregion

        #region Commands (Neu, Bearbeiten, Speichern, Abbrechen, Löschen)

        // ... (NewTool bleibt gleich) ...
        [RelayCommand]
        private void NewTool()
        {
            EditingTool = new Werkzeug
            {
                Radius = null,
                Steigung = null,
                Spitzenwinkel = null,
                Durchmesser = null,
                Breite = null,
                MaxStechtiefe = null
            };

            ToolName = string.Empty;
            RadiusInputString = string.Empty;
            PitchInputString = string.Empty;
            SpitzenwinkelInputString = string.Empty;
            DurchmesserInputString = string.Empty;
            BreiteInputString = string.Empty;
            MaxStechtiefeInputString = string.Empty;

            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsInEditMode = true;

            IsRadiusRequired = false;
            IsPitchRequired = false;
            IsSpitzenwinkelRequired = false;
            IsDurchmesserRequired = false;
            IsBreiteRequired = false;
            IsMaxStechtiefeRequired = false;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;
        }


        // ... (EditTool bleibt gleich) ...
        [RelayCommand]
        private void EditTool(Werkzeug? toolFromClick)
        {
            var toolToEdit = toolFromClick ?? SelectedTool;

            if (toolToEdit == null) return;

            if (toolFromClick != null)
            {
                SelectedTool = toolToEdit;
            }

            if (IsSelectionMode)
            {
                _onToolSelectedCallback?.Invoke(toolToEdit);
            }
            else
            {
                EditingTool = new Werkzeug
                {
                    WerkzeugID = toolToEdit.WerkzeugID,
                    Name = toolToEdit.Name,
                    Beschreibung = toolToEdit.Beschreibung,
                    Steigung = toolToEdit.Steigung,
                    Spitzenwinkel = toolToEdit.Spitzenwinkel,
                    Radius = toolToEdit.Radius,
                    Durchmesser = toolToEdit.Durchmesser,
                    Breite = toolToEdit.Breite,
                    MaxStechtiefe = toolToEdit.MaxStechtiefe,
                    Unterkategorie = toolToEdit.Unterkategorie,
                    WerkzeugUnterkategorieID = toolToEdit.WerkzeugUnterkategorieID
                };

                ToolName = EditingTool.Name;

                RadiusInputString = EditingTool.Radius?.ToString("G", CultureInfo.CurrentCulture);
                PitchInputString = EditingTool.Steigung?.ToString("G", CultureInfo.CurrentCulture);
                SpitzenwinkelInputString = EditingTool.Spitzenwinkel?.ToString("G", CultureInfo.CurrentCulture);
                DurchmesserInputString = EditingTool.Durchmesser?.ToString("G", CultureInfo.CurrentCulture);
                BreiteInputString = EditingTool.Breite?.ToString("G", CultureInfo.CurrentCulture);
                MaxStechtiefeInputString = EditingTool.MaxStechtiefe?.ToString("G", CultureInfo.CurrentCulture);

                if (toolToEdit.Unterkategorie?.Kategorie != null)
                {
                    SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == toolToEdit.Unterkategorie.Kategorie.WerkzeugKategorieID);
                }
                else
                {
                    using var context = new NcSetupContext();
                    var unterkat = context.WerkzeugUnterkategorien.Find(toolToEdit.WerkzeugUnterkategorieID);
                    if (unterkat != null)
                    {
                        SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == unterkat.WerkzeugKategorieID);
                    }
                    else
                    {
                        SelectedKategorie = null;
                    }
                }

                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u => u.WerkzeugUnterkategorieID == toolToEdit.WerkzeugUnterkategorieID);

                IsInEditMode = true;
                IsUnterkategorieEnabled = true;
                IsToolDetailsEnabled = true;
            }
        }


        // ... (ParseNullableDouble bleibt gleich) ...
        private bool ParseNullableDouble(string input, bool isRequired, string fieldName, out double? result)
        {
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
            if (EditingTool == null || SelectedUnterkategorie == null || string.IsNullOrWhiteSpace(ToolName))
            {
                MessageBox.Show("Bitte wählen Sie eine Kategorie, einen Typ aus und geben Sie einen Namen ein.", "Fehlende Eingabe");
                return;
            }

            // ... (Parsing-Logik bleibt gleich) ...
            double? finalRadius;
            if (!ParseNullableDouble(RadiusInputString, IsRadiusRequired, "Radius", out finalRadius)) return;
            double? finalPitch;
            if (!ParseNullableDouble(PitchInputString, IsPitchRequired, "Steigung", out finalPitch)) return;
            double? finalSpitzenwinkel;
            if (!ParseNullableDouble(SpitzenwinkelInputString, IsSpitzenwinkelRequired, "Spitzenwinkel", out finalSpitzenwinkel)) return;
            double? finalDurchmesser;
            if (!ParseNullableDouble(DurchmesserInputString, IsDurchmesserRequired, "Durchmesser", out finalDurchmesser)) return;
            double? finalBreite;
            if (!ParseNullableDouble(BreiteInputString, IsBreiteRequired, "Breite", out finalBreite)) return;
            double? finalMaxStechtiefe;
            if (!ParseNullableDouble(MaxStechtiefeInputString, IsMaxStechtiefeRequired, "Max. Stechtiefe", out finalMaxStechtiefe)) return;

            EditingTool.Radius = finalRadius;
            EditingTool.Steigung = finalPitch;
            EditingTool.Spitzenwinkel = finalSpitzenwinkel;
            EditingTool.Durchmesser = finalDurchmesser;
            EditingTool.Breite = finalBreite;
            EditingTool.MaxStechtiefe = finalMaxStechtiefe;
            EditingTool.Name = ToolName;

            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();

                if (EditingTool.WerkzeugID == 0)
                {
                    EditingTool.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                    EditingTool.Unterkategorie = null!; // Navigationseigenschaft nullen
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
                        toolToUpdate.Spitzenwinkel = EditingTool.Spitzenwinkel;
                        toolToUpdate.Radius = EditingTool.Radius;
                        toolToUpdate.Durchmesser = EditingTool.Durchmesser;
                        toolToUpdate.Breite = EditingTool.Breite;
                        toolToUpdate.MaxStechtiefe = EditingTool.MaxStechtiefe;
                        toolToUpdate.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                    }
                }

                context.SaveChanges();
                LoadTools(); // Diese Methode hat jetzt ein try-catch
                IsInEditMode = false;
                EditingTool = null;
                ToolName = string.Empty;
                SelectedTool = null;
                Cancel(); // Setzt Filter und Bearbeitungsstatus zurück
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Speichern eines Werkzeugs");
                MessageBox.Show($"Fehler beim Speichern des Werkzeugs:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ... (Cancel bleibt gleich) ...
        [RelayCommand]
        private void Cancel()
        {
            EditingTool = null;
            IsInEditMode = false;

            IsRadiusRequired = false;
            IsPitchRequired = false;
            IsSpitzenwinkelRequired = false;
            IsDurchmesserRequired = false;
            IsBreiteRequired = false;
            IsMaxStechtiefeRequired = false;

            RadiusInputString = string.Empty;
            PitchInputString = string.Empty;
            SpitzenwinkelInputString = string.Empty;
            DurchmesserInputString = string.Empty;
            BreiteInputString = string.Empty;
            MaxStechtiefeInputString = string.Empty;

            ToolName = string.Empty;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;

            if (IsSelectionMode)
            {
                ApplyFavoritFilter(_initialFavoritKategorie, _initialFavoritUnterkategorie);
            }
            else
            {
                SelectedKategorie = null;
                SelectedUnterkategorie = null;
            }
        }


        [RelayCommand]
        private void DeleteTool()
        {
            if (SelectedTool == null) return;

            var standardToolIds = new List<int> { 1, 2, 3, 4, 5 };
            if (standardToolIds.Contains(SelectedTool.WerkzeugID))
            {
                MessageBox.Show($"Das Werkzeug '{SelectedTool.Name}' ist ein Standardwerkzeug und kann nicht gelöscht werden.",
                                "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Möchten Sie das Werkzeug '{SelectedTool.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // --- NEU: try-catch ---
                try
                {
                    using var context = new NcSetupContext();

                    bool isStandardTool = context.StandardWerkzeugZuweisungen.Any(z => z.WerkzeugID == SelectedTool.WerkzeugID);
                    if (isStandardTool)
                    {
                        MessageBox.Show($"Das Werkzeug '{SelectedTool.Name}' kann nicht gelöscht werden, da es einer Maschine als Standardwerkzeug zugewiesen ist.",
                                        "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var toolToDelete = context.Werkzeuge.Find(SelectedTool.WerkzeugID);
                    if (toolToDelete != null)
                    {
                        context.Werkzeuge.Remove(toolToDelete);
                        context.SaveChanges();
                    }
                    LoadTools();
                }
                catch (Exception ex)
                {
                    LoggingService.LogException(ex, "Fehler beim Löschen eines Werkzeugs");
                    MessageBox.Show($"Fehler beim Löschen des Werkzeugs:\n{ex.Message}", "Löschfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}