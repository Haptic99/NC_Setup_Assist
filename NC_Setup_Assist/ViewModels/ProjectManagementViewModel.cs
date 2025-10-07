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
    public partial class ProjectManagementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<Projekt> Projekte { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenProjectCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
        private Projekt? _selectedProjekt;

        public ProjectManagementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadProjekte();
        }

        private void LoadProjekte()
        {
            Projekte.Clear();
            using var context = new NcSetupContext();

            // Lädt alle Projekte, inklusive der zugehörigen Maschine und den NC-Programmen
            var projekteFromDb = context.Projekte
                                        .Include(p => p.ZugehoerigeMaschine)
                                        .Include(p => p.NCProgramme) // Wichtig für die Anzeige der Anzahl der Programme
                                        .ToList();

            foreach (var projekt in projekteFromDb)
            {
                Projekte.Add(projekt);
            }
        }

        private bool CanExecuteProjectCommand()
        {
            return SelectedProjekt != null;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteProjectCommand))]
        private void OpenProject(Projekt? projekt)
        {
            if (projekt?.NCProgramme.Any() == true)
            {
                // Wählt das ERSTE NC-Programm im Projekt zum Öffnen der Analyse-Ansicht
                var ncProgramm = projekt.NCProgramme.First();
                _mainViewModel.NavigateTo(new AnalysisViewModel(ncProgramm, _mainViewModel));
            }
            else
            {
                MessageBox.Show($"Projekt '{projekt?.Name}' enthält kein NC-Programm.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteProjectCommand))]
        private void DeleteProject()
        {
            if (SelectedProjekt == null) return;

            var result = MessageBox.Show($"Möchten Sie das Projekt '{SelectedProjekt.Name}' und alle zugehörigen Daten (NC-Programme, Werkzeugeinsätze) wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var context = new NcSetupContext();
                var projektToDelete = context.Projekte.Find(SelectedProjekt.ProjektID);
                if (projektToDelete != null)
                {
                    // Alle abhängigen NCProgramme und WerkzeugEinsaetze werden durch Cascade Delete (DbContext-Einstellung) gelöscht
                    context.Projekte.Remove(projektToDelete);
                    context.SaveChanges();
                }
                LoadProjekte();
            }
        }
    }
}