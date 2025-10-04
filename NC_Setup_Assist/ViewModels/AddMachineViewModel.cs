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
    public partial class AddMachineViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private Maschine _newMachine = new Maschine();

        [ObservableProperty]
        private Standort? _selectedStandort;

        public ObservableCollection<Standort> Standorte { get; } = new();

        public AddMachineViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadStandorte();
        }

        private void LoadStandorte()
        {
            Standorte.Clear();
            using var context = new NcSetupContext();
            var standorteFromDb = context.Standorte.ToList();
            foreach (var standort in standorteFromDb)
            {
                Standorte.Add(standort);
            }
        }

        partial void OnSelectedStandortChanged(Standort? value)
        {
            if (value != null)
            {
                NewMachine.StandortID = value.StandortID;
            }
        }

        [RelayCommand]
        private void SaveMachine()
        {
            if (string.IsNullOrWhiteSpace(NewMachine.Name) || SelectedStandort == null)
            {
                MessageBox.Show("Bitte füllen Sie alle erforderlichen Felder aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var context = new NcSetupContext();
            context.Maschinen.Add(NewMachine);
            context.SaveChanges();

            MessageBox.Show("Maschine erfolgreich hinzugefügt.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            _mainViewModel.NavigateBack();
        }

        [RelayCommand]
        private void Cancel()
        {
            _mainViewModel.NavigateBack();
        }
    }
}