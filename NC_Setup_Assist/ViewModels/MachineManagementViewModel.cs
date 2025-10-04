using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MachineManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public MachineManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void AddMachine()
        {
            _mainViewModel.NavigateTo(new AddMachineViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void ManageExistingMachines()
        {
            MessageBox.Show("Funktion 'Bestehende Maschinen verwalten' wird noch implementiert.");
        }
    }
}