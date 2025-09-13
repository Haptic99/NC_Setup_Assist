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

        // --- NEU: Ein einziges Suchfeld für alles ---
        [ObservableProperty]
        private string? _searchTerm;

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

        // NEU: Angepasste Filter-Logik
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

        // NEU: Ein einziger Trigger für das Suchfeld
        partial void OnSearchTermChanged(string? value) => ApplyFilter();

        #endregion

        // ... der Rest der Datei (Kaskadierende Dropdowns, Commands) bleibt unverändert ...
        #region Kaskadierende Dropdown-Logik
        partial void OnSelectedToolChanged(Werkzeug? value)
        {
            if (value?.Unterkategorie?.Kategorie != null)
            {
                SelectedKategorie = Kategorien.FirstOrDefault(k => k.WerkzeugKategorieID == value.Unterkategorie.Kategorie.WerkzeugKategorieID);
            }
            else
            {
                SelectedKategorie = null;
            }
        }

        partial void OnSelectedKategorieChanged(WerkzeugKategorie? value)
        {
            using var context = new NcSetupContext();
            Unterkategorien.Clear();
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
        #endregion

        #region Commands (Neu, Bearbeiten, Speichern, Abbrechen, Löschen)
        [RelayCommand]
        private void NewTool()
        {
            EditingTool = new Werkzeug();
            IsInEditMode = true;
        }

        [RelayCommand]
        private void EditTool()
        {
            if (SelectedTool != null)
            {
                EditingTool = new Werkzeug
                {
                    WerkzeugID = SelectedTool.WerkzeugID,
                    Name = SelectedTool.Name,
                    Beschreibung = SelectedTool.Beschreibung,
                    Unterkategorie = SelectedTool.Unterkategorie,
                    WerkzeugUnterkategorieID = SelectedTool.WerkzeugUnterkategorieID
                };
                IsInEditMode = true;
            }
        }

        [RelayCommand]
        private void SaveTool()
        {
            if (EditingTool == null || EditingTool.Unterkategorie == null)
            {
                MessageBox.Show("Bitte wählen Sie eine Kategorie und einen Typ aus.", "Fehlende Eingabe");
                return;
            }

            using var context = new NcSetupContext();

            if (EditingTool.WerkzeugID == 0)
            {
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
                    toolToUpdate.WerkzeugUnterkategorieID = EditingTool.Unterkategorie.WerkzeugUnterkategorieID;
                }
            }

            context.SaveChanges();
            LoadTools();
            IsInEditMode = false;
            EditingTool = null;
            SelectedTool = null;
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingTool = null;
            IsInEditMode = false;
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