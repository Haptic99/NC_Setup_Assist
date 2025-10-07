using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class StandortManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<Standort> Standorte { get; } = new();

        [ObservableProperty]
        private Standort? _selectedStandort;

        [ObservableProperty]
        private Standort? _editingStandort;

        [ObservableProperty]
        private bool _isInEditMode;

        public StandortManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadData();
        }

        private void LoadData()
        {
            Standorte.Clear();

            using var context = new NcSetupContext();

            // Lade Standorte und beziehe die zugehörigen Maschinen mit ein
            var standorteFromDb = context.Standorte
                                         .Include(s => s.Maschinen)
                                         .OrderBy(s => s.Name)
                                         .ToList();
            foreach (var standort in standorteFromDb)
            {
                Standorte.Add(standort);
            }
        }

        [RelayCommand]
        private void NewStandort()
        {
            EditingStandort = new Standort();
            IsInEditMode = true;
        }

        [RelayCommand]
        private void EditStandort()
        {
            if (SelectedStandort == null) return;

            // Erstelle eine Kopie für die Bearbeitung, um die Originaldaten nicht direkt zu ändern
            EditingStandort = new Standort
            {
                StandortID = SelectedStandort.StandortID,
                Name = SelectedStandort.Name,
                Strasse = SelectedStandort.Strasse,
                Hausnummer = SelectedStandort.Hausnummer,
                PLZ = SelectedStandort.PLZ,
                Stadt = SelectedStandort.Stadt,
                Maschinen = SelectedStandort.Maschinen
            };
            IsInEditMode = true;
        }

        [RelayCommand]
        private void SaveStandort()
        {
            // HIER WURDE DIE PRÜFUNG ERWEITERT
            if (EditingStandort == null ||
                string.IsNullOrWhiteSpace(EditingStandort.Name) ||
                string.IsNullOrWhiteSpace(EditingStandort.Strasse) ||
                string.IsNullOrWhiteSpace(EditingStandort.Hausnummer) ||
                string.IsNullOrWhiteSpace(EditingStandort.PLZ) ||
                string.IsNullOrWhiteSpace(EditingStandort.Stadt))
            {
                MessageBox.Show("Bitte füllen Sie alle Felder für den Standort aus.", "Fehlende Angaben", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var context = new NcSetupContext();
            if (EditingStandort.StandortID == 0) // Neuer Standort
            {
                context.Standorte.Add(EditingStandort);
            }
            else // Bestehenden Standort aktualisieren
            {
                var standortToUpdate = context.Standorte.Find(EditingStandort.StandortID);
                if (standortToUpdate != null)
                {
                    standortToUpdate.Name = EditingStandort.Name;
                    standortToUpdate.Strasse = EditingStandort.Strasse;
                    standortToUpdate.Hausnummer = EditingStandort.Hausnummer;
                    standortToUpdate.PLZ = EditingStandort.PLZ;
                    standortToUpdate.Stadt = EditingStandort.Stadt;
                }
            }
            context.SaveChanges();

            IsInEditMode = false;
            EditingStandort = null;
            LoadData(); // Lade die Daten neu, um die Änderungen anzuzeigen
        }

        [RelayCommand]
        private void DeleteStandort()
        {
            if (SelectedStandort == null) return;

            // Sicherheitsabfrage
            if (SelectedStandort.Maschinen.Any())
            {
                MessageBox.Show($"Der Standort '{SelectedStandort.Name}' kann nicht gelöscht werden, da ihm noch Maschinen zugewiesen sind.", "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Möchten Sie den Standort '{SelectedStandort.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var context = new NcSetupContext();
                var standortToDelete = context.Standorte.Find(SelectedStandort.StandortID);
                if (standortToDelete != null)
                {
                    context.Standorte.Remove(standortToDelete);
                    context.SaveChanges();
                }
                LoadData();
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            EditingStandort = null;
            IsInEditMode = false;
        }
    }
}