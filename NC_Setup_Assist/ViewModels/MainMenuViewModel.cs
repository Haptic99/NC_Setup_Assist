using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MainMenuViewModel : ViewModelBase
    {
        // ... (MainViewModel Referenz und Konstruktor bleiben gleich) ...
        private readonly MainViewModel _mainViewModel;

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
            _mainViewModel.NavigateTo(new ProjectManagementViewModel(_mainViewModel));
        }

        [RelayCommand]
        private void OpenSettings()
        {
            _mainViewModel.NavigateTo(new SettingsDashboardViewModel(_mainViewModel));
        }

        // --- NEUE METHODE ---
        [RelayCommand]
        private void OpenAbout()
        {
            _mainViewModel.NavigateTo(new AboutViewModel(_mainViewModel));
        }
    }
}