// NC_Setup_Assist/ViewModels/AddMachineViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services; // <-- NEU
using System; // <-- NEU
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class AddMachineViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private Maschine _newMachine = new Maschine();

        [ObservableProperty]
        private Standort? _selectedStandort;

        [ObservableProperty] // NEU
        private Hersteller? _selectedHersteller;

        public ObservableCollection<Standort> Standorte { get; } = new();
        public ObservableCollection<Hersteller> Hersteller { get; } = new(); // NEU

        public AddMachineViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadStandorte();
            LoadHersteller();
        }

        private void LoadStandorte()
        {
            // --- NEU: try-catch ---
            try
            {
                Standorte.Clear();
                using var context = new NcSetupContext();
                var standorteFromDb = context.Standorte.ToList();
                foreach (var standort in standorteFromDb)
                {
                    Standorte.Add(standort);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Standorte in AddMachineViewModel");
                MessageBox.Show($"Fehler beim Laden der Standorte:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHersteller() // NEU
        {
            // --- NEU: try-catch ---
            try
            {
                Hersteller.Clear();
                using var context = new NcSetupContext();
                var herstellerFromDb = context.Hersteller.ToList();
                foreach (var hersteller in herstellerFromDb)
                {
                    Hersteller.Add(hersteller);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Hersteller in AddMachineViewModel");
                MessageBox.Show($"Fehler beim Laden der Hersteller:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        partial void OnSelectedStandortChanged(Standort? value)
        {
            if (value != null)
            {
                NewMachine.StandortID = value.StandortID;
            }
        }

        partial void OnSelectedHerstellerChanged(Hersteller? value) // NEU
        {
            if (value != null)
            {
                NewMachine.HerstellerID = value.HerstellerID;
            }
        }

        [RelayCommand]
        private void SaveMachine()
        {
            if (string.IsNullOrWhiteSpace(NewMachine.Name) || SelectedStandort == null || SelectedHersteller == null) // NEU
            {
                MessageBox.Show("Bitte füllen Sie alle erforderlichen Felder aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- NEU: try-catch ---
            try
            {
                using var context = new NcSetupContext();
                context.Maschinen.Add(NewMachine);
                context.SaveChanges();

                MessageBox.Show("Maschine erfolgreich hinzugefügt.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainViewModel.NavigateBack();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Speichern einer neuen Maschine");
                MessageBox.Show($"Fehler beim Speichern der Maschine:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _mainViewModel.NavigateBack();
        }
    }
}