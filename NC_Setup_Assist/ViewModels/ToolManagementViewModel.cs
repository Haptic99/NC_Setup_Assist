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
        private bool _isRadiusRequired; // Gesteuert durch Unterkategorie

        [ObservableProperty]
        private string? _radiusInputString; // Wert für Radius

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
        private readonly string? _initialFilter; // NEU


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

        // ANGEPASSTER Konstruktor für Auswahlmodus (ersetzt den alten)
        public ToolManagementViewModel(MainViewModel mainViewModel, Action<Werkzeug> onToolSelectedCallback, string? initialFilter = null) : this(mainViewModel)
        {
            _onToolSelectedCallback = onToolSelectedCallback;
            IsSelectionMode = true;
            _initialFilter = initialFilter;

            // NEU: Wenn ein Filter gesetzt ist, wende ihn an.
            if (!string.IsNullOrEmpty(_initialFilter))
            {
                // Finde die Kategorie, die dem Filter "Fräsen" entspricht.
                // Annahme: Der Filter-String "Fräsen" entspricht einem Kategorienamen.
                var matchingKategorie = Kategorien.FirstOrDefault(k => k.Name.Equals(_initialFilter, StringComparison.OrdinalIgnoreCase));
                if (matchingKategorie != null)
                {
                    SelectedKategorie = matchingKategorie;
                    // Das Setzen von SelectedKategorie löst OnSelectedKategorieChanged aus,
                    // was wiederum ApplyFilter() aufruft.
                }
            }
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

        // --- HIER IST DIE KORREKTUR ---
        private void ApplyFilter()
        {
            IEnumerable<Werkzeug> tempFiltered = _allTools;

            // --- KORREKTUR: Lokale Kopien erstellen, um Race Conditions zu verhindern ---
            // Wir "frieren" die Werte zu Beginn der Methode ein.
            var currentKategorie = SelectedKategorie;
            var currentUnterkategorie = SelectedUnterkategorie;
            var currentSearchTerm = SearchTerm;
            // --- ENDE KORREKTUR ---


            // 1. Filter nach Kategorie
            if (currentKategorie != null)
            {
                // Verwende die lokale Kopie 'currentKategorie'
                tempFiltered = tempFiltered.Where(w => w.Unterkategorie?.Kategorie?.WerkzeugKategorieID == currentKategorie.WerkzeugKategorieID);
            }

            // 2. Filter nach Unterkategorie
            if (currentUnterkategorie != null)
            {
                // Verwende die lokale Kopie 'currentUnterkategorie'
                tempFiltered = tempFiltered.Where(w => w.WerkzeugUnterkategorieID == currentUnterkategorie.WerkzeugUnterkategorieID);
            }

            // 3. Filter nach Suchbegriff
            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                // Verwende die lokale Kopie 'currentSearchTerm'
                tempFiltered = tempFiltered.Where(w =>
                    (w.Name?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Unterkategorie?.Name?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Beschreibung?.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            // Liste aktualisieren
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
            // --- KORREKTUR (aus vorheriger Antwort) ---
            // Der Rumpf dieser Methode wurde entfernt.
            // Das Auswählen eines Werkzeugs in der Liste ändert die Filter-Dropdowns nicht mehr.
            // Die Dropdowns werden nur noch im 'EditTool'-Befehl gesetzt.
            // --- ENDE KORREKTUR ---
        }

        partial void OnSelectedKategorieChanged(WerkzeugKategorie? value)
        {
            // (Unverändert, lädt Unterkategorien)
            using var context = new NcSetupContext();
            Unterkategorien.Clear();
            SelectedUnterkategorie = null; // WICHTIG: Unterkategorie zurücksetzen
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

            ApplyFilter(); // NEU: Filter anwenden
        }

        partial void OnSelectedUnterkategorieChanged(WerkzeugUnterkategorie? value)
        {
            // (Unverändert)
            IsRadiusRequired = (value?.BenötigtRadius == true);
            IsPitchRequired = (value?.BenötigtSteigung == true);
            IsPlattenwinkelRequired = (value?.BenötigtPlattenwinkel == true);

            if (value != null)
            {
                IsToolDetailsEnabled = true;
                if (EditingTool?.WerkzeugID == 0) // Nur bei "Neu" zurücksetzen
                {
                    RadiusInputString = string.Empty;
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

            ApplyFilter(); // NEU: Filter anwenden
        }

        partial void OnRadiusInputStringChanged(string? value)
        {
            UpdateToolName();
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

            // Nur den Namen aktualisieren, wenn es ein NEUES Werkzeug ist ODER
            // wenn der Benutzer die Unterkategorie eines bestehenden Werkzeugs ändert.
            if (EditingTool.WerkzeugID == 0 || (EditingTool.WerkzeugUnterkategorieID != SelectedUnterkategorie.WerkzeugUnterkategorieID))
            {
                string baseName = SelectedUnterkategorie.Name;
                var sb = new StringBuilder(baseName);

                if (IsRadiusRequired)
                {
                    string radiusDisplay = (RadiusInputString ?? "").Trim().Replace(',', '.');
                    if (!string.IsNullOrEmpty(radiusDisplay))
                    {
                        sb.Append($" R={radiusDisplay}");
                    }
                }

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
            RadiusInputString = string.Empty;
            PitchInputString = string.Empty;
            PlattenwinkelInputString = string.Empty;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;
            IsInEditMode = true;
            IsRadiusRequired = false;
            IsPitchRequired = false;
            IsPlattenwinkelRequired = false;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;
        }

        // --- HIER BEGINNT DIE ÄNDERUNG ---

        [RelayCommand]
        private void EditTool(Werkzeug? toolFromClick) // Parameter hinzugefügt
        {
            // Wir verwenden das Werkzeug aus dem Klick oder fallen auf das ausgewählte Werkzeug zurück
            var toolToEdit = toolFromClick ?? SelectedTool;

            if (toolToEdit == null) return; // Schutz-Check

            // Wichtig: Stellen Sie sicher, dass das SelectedTool synchronisiert ist
            if (toolFromClick != null)
            {
                SelectedTool = toolToEdit;
            }

            if (IsSelectionMode)
            {
                _onToolSelectedCallback?.Invoke(toolToEdit); // toolToEdit verwenden
            }
            else
            {
                // Erstelle die Kopie basierend auf toolToEdit
                EditingTool = new Werkzeug
                {
                    WerkzeugID = toolToEdit.WerkzeugID,
                    Name = toolToEdit.Name,
                    Beschreibung = toolToEdit.Beschreibung,
                    Steigung = toolToEdit.Steigung,
                    Plattenwinkel = toolToEdit.Plattenwinkel,
                    Unterkategorie = toolToEdit.Unterkategorie,
                    WerkzeugUnterkategorieID = toolToEdit.WerkzeugUnterkategorieID
                };

                ToolName = EditingTool.Name;

                RadiusInputString = EditingTool.Radius?.ToString("G", CultureInfo.CurrentCulture);
                PitchInputString = EditingTool.Steigung?.ToString("G", CultureInfo.CurrentCulture);
                PlattenwinkelInputString = EditingTool.Plattenwinkel?.ToString("G", CultureInfo.CurrentCulture);


                if (toolToEdit.Unterkategorie?.Kategorie != null)
                {
                    // Normaler Weg
                    SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == toolToEdit.Unterkategorie.Kategorie.WerkzeugKategorieID);
                }
                else
                {
                    // Fallback, falls die Navigationseigenschaft fehlt.
                    using var context = new NcSetupContext();
                    var unterkat = context.WerkzeugUnterkategorien.Find(toolToEdit.WerkzeugUnterkategorieID);
                    if (unterkat != null)
                    {
                        SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == unterkat.WerkzeugKategorieID);
                    }
                    else
                    {
                        SelectedKategorie = null; // Sollte nicht passieren
                    }
                }

                // Diese Zuweisung funktioniert jetzt
                SelectedUnterkategorie = Unterkategorien.FirstOrDefault(u => u.WerkzeugUnterkategorieID == toolToEdit.WerkzeugUnterkategorieID);

                IsInEditMode = true;
                IsUnterkategorieEnabled = true;
                IsToolDetailsEnabled = true;
            }
        }

        // --- HIER ENDET DIE ÄNDERUNG ---


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

            double? finalRadius;
            if (!ParseNullableDouble(RadiusInputString, IsRadiusRequired, "Radius", out finalRadius))
            {
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

            EditingTool.Radius = finalRadius;
            EditingTool.Steigung = finalPitch;
            EditingTool.Plattenwinkel = finalPlattenwinkel;
            EditingTool.Name = ToolName;

            using var context = new NcSetupContext();

            if (EditingTool.WerkzeugID == 0)
            {
                EditingTool.WerkzeugUnterkategorieID = SelectedUnterkategorie.WerkzeugUnterkategorieID;
                context.WerkzeugUnterkategorien.Attach(SelectedUnterkategorie); // Sagen EF, dass Unterkat. existiert
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
            Cancel(); // Setzt Filter und Bearbeitungsstatus zurück
        }

        [RelayCommand]
        private void Cancel()
        {
            // (Unverändert)
            EditingTool = null;
            IsInEditMode = false;
            SelectedKategorie = null;
            SelectedUnterkategorie = null;

            IsRadiusRequired = false;
            IsPitchRequired = false;
            IsPlattenwinkelRequired = false;
            RadiusInputString = string.Empty;
            PitchInputString = string.Empty;
            PlattenwinkelInputString = string.Empty;
            ToolName = string.Empty;

            IsUnterkategorieEnabled = false;
            IsToolDetailsEnabled = false;

            // Wende den Initialfilter (z.B. "Fräsen") wieder an, falls wir im Auswahlmodus sind
            if (IsSelectionMode && !string.IsNullOrEmpty(_initialFilter))
            {
                var matchingKategorie = Kategorien.FirstOrDefault(k => k.Name.Equals(_initialFilter, StringComparison.OrdinalIgnoreCase));
                if (matchingKategorie != null)
                {
                    SelectedKategorie = matchingKategorie;
                }
            }
        }

        [RelayCommand]
        private void DeleteTool()
        {
            if (SelectedTool == null) return;

            // --- NEUE PRÜFUNG ---
            var standardToolIds = new List<int> { 1,2,3,4,5 };
            if (standardToolIds.Contains(SelectedTool.WerkzeugID))
            {
                MessageBox.Show($"Das Werkzeug '{SelectedTool.Name}' ist ein Standardwerkzeug und kann nicht gelöscht werden.",
                                "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // --- ENDE PRÜFUNG ---

            var result = MessageBox.Show($"Möchten Sie das Werkzeug '{SelectedTool.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var context = new NcSetupContext();

                // Zuerst prüfen, ob das Werkzeug in StandardWerkzeugZuweisungen verwendet wird
                bool isStandardTool = context.StandardWerkzeugZuweisungen.Any(z => z.WerkzeugID == SelectedTool.WerkzeugID);
                if (isStandardTool)
                {
                    MessageBox.Show($"Das Werkzeug '{SelectedTool.Name}' kann nicht gelöscht werden, da es einer Maschine als Standardwerkzeug zugewiesen ist.",
                                    "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // (Die Prüfung auf WerkzeugEinsaetze ist dank Cascade Delete nicht zwingend, 
                // aber die Prüfung auf StandardWerkzeugZuweisungen ist wichtig.)

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