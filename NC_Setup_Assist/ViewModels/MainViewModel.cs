// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;

namespace NC_Setup_Assist.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        // NEU: Hält den Namen der aktiven Haupt-View für die Sidebar-Buttons
        [ObservableProperty]
        private string _activeViewName = "Dashboard";

        private Stack<ViewModelBase> _navigationHistory = new Stack<ViewModelBase>();

        public MainViewModel()
        {
            // Die Start-View festlegen
            var startViewModel = new MainMenuViewModel(this);
            CurrentViewModel = startViewModel;
            UpdateActiveViewName(startViewModel); // NEU: Initialen Namen setzen
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            if (CurrentViewModel != null)
            {
                _navigationHistory.Push(CurrentViewModel);
            }
            CurrentViewModel = viewModel;

            UpdateActiveViewName(viewModel); // NEU: Aktiven View-Namen aktualisieren
            NavigateBackCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanNavigateBack))]
        public void NavigateBack()
        {
            if (_navigationHistory.Any())
            {
                CurrentViewModel = _navigationHistory.Pop();
                UpdateActiveViewName(CurrentViewModel); // NEU: Aktiven View-Namen aktualisieren
                NavigateBackCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanNavigateBack()
        {
            return _navigationHistory.Any();
        }

        // --- NEUE COMMANDS FÜR DIE SIDEBAR ---
        [RelayCommand]
        private void NavigateDashboard()
        {
            // Verhindert unnötiges Neuladen, wenn bereits aktiv
            if (CurrentViewModel is not MainMenuViewModel)
            {
                NavigateTo(new MainMenuViewModel(this));
            }
        }

        [RelayCommand]
        private void NavigateProjects()
        {
            if (CurrentViewModel is not ProjectManagementViewModel)
            {
                NavigateTo(new ProjectManagementViewModel(this));
            }
        }

        [RelayCommand]
        private void NavigateSettings()
        {
            if (CurrentViewModel is not SettingsDashboardViewModel)
            {
                NavigateTo(new SettingsDashboardViewModel(this));
            }
        }

        // --- NEUE HELPER-METHODE ---
        /// <summary>
        /// Aktualisiert die ActiveViewName-Eigenschaft basierend auf dem Typ des ViewModels,
        /// damit die RadioButtons in der Sidebar korrekt synchronisiert werden.
        /// </summary>
        private void UpdateActiveViewName(ViewModelBase? vm)
        {
            if (vm is MainMenuViewModel or NewProjectViewModel or AnalysisViewModel)
            {
                // Dashboard ist aktiv für das Hauptmenü und untergeordnete Projektansichten
                ActiveViewName = "Dashboard";
            }
            else if (vm is ProjectManagementViewModel)
            {
                ActiveViewName = "Projects";
            }
            else if (vm is SettingsDashboardViewModel or ToolManagementViewModel or MachineManagementViewModel or StandortManagementViewModel or HerstellerManagementViewModel or KategorieManagementViewModel or UnterkategorieManagementViewModel or StandardToolsManagementViewModel)
            {
                // Alle Einstellungs-Dashboards und deren Unteransichten
                ActiveViewName = "Settings";
            }
            // Bei "About" oder anderen Ansichten bleibt die Auswahl einfach wie sie war.
        }
    }
}