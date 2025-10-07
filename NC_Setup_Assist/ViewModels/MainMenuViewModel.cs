using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MainMenuViewModel : ViewModelBase
    {
        // Eine private Referenz auf den "Dirigenten"
        private readonly MainViewModel _mainViewModel;

        // Der Konstruktor empfängt jetzt den MainViewModel, um mit ihm reden zu können
        public MainMenuViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void CreateNewProject()
        {
            _mainViewModel.NavigateTo(new NewProjectViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void ManageProjects()
        {
            // NEU: Navigation zur Projektverwaltung
            _mainViewModel.NavigateTo(new ProjectManagementViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void OpenSettings()
        {
            _mainViewModel.NavigateTo(new SettingsDashboardViewModel(_mainViewModel));
        }
    }
}