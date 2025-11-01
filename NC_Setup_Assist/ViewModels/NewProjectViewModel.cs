// NC_Setup_Assist/ViewModels/NewProjectViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services;
using System; // <-- NEU
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class NewProjectViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private string? _ncFilePath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private string? _projectName;

        // --- ANGEPASSTE EIGENSCHAFTEN ---
        public ObservableCollection<Standort> Standorte { get; } = new();
        public ObservableCollection<Maschine> Maschinen { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private Standort? _selectedStandort;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private Maschine? _selectedMaschine;


        public NewProjectViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadStandorte(); // Lädt die Standorte beim Start
        }

        // --- NEUE METHODE: Wird aufgerufen, wenn sich die Auswahl des Standorts ändert ---
        partial void OnSelectedStandortChanged(Standort? value)
        {
            LoadMaschinen(value);
        }

        [RelayCommand]
        private void BrowseNcFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "NC-Programme (*.nc;*.NC)|*.nc;*.NC|Alle Dateien (*.*)|*.*",
                Title = "NC-Programm auswählen"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                NcFilePath = openFileDialog.FileName;
                // Extrahiert den Dateinamen ohne Erweiterung als Projektnamen-Vorschlag
                ProjectName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            }
        }

        private bool CanCreateProject()
        {
            // Ein Projekt kann nur erstellt werden, wenn alle Felder ausgefüllt sind
            return !string.IsNullOrWhiteSpace(NcFilePath) &&
                   !string.IsNullOrWhiteSpace(ProjectName) &&
                   SelectedStandort != null &&
                   SelectedMaschine != null;
        }

        [RelayCommand(CanExecute = nameof(CanCreateProject))]
        private void CreateProject()
        {
            if (string.IsNullOrWhiteSpace(NcFilePath) ||
                string.IsNullOrWhiteSpace(ProjectName) ||
                SelectedStandort == null ||
                SelectedMaschine == null)
            {
                MessageBox.Show("Bitte füllen Sie alle Felder aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Der Parser hat bereits sein eigenes try-catch
            var parser = new NcCodeParserService();
            var werkzeugEinsaetze = parser.Parse(NcFilePath);

            // --- NEU: try-catch für die gesamte Transaktion ---
            try
            {
                using var context = new NcSetupContext();

                // 1. Das neue Projekt wird mit der ID der ausgewählten Maschine erstellt
                var neuesProjekt = new Projekt
                {
                    Name = ProjectName,
                    MaschineID = SelectedMaschine.MaschineID
                };
                context.Projekte.Add(neuesProjekt);
                context.SaveChanges(); // Speichern, um die neue ProjektID zu erhalten

                // 2. Jetzt das NCProgramm erstellen und mit dem Projekt verknüpfen
                var neuesProgramm = new NCProgramm
                {
                    ZeichnungsNummer = ProjectName ?? Path.GetFileNameWithoutExtension(NcFilePath),
                    Bezeichnung = Path.GetFileName(NcFilePath),
                    DateiPfad = NcFilePath,
                    MaschineID = SelectedMaschine.MaschineID,
                    // KORREKTUR: Weise die ProjektID explizit zu.
                    ProjektID = neuesProjekt.ProjektID // <--- WICHTIGE KORREKTUR!
                };
                context.NCProgramme.Add(neuesProgramm);
                context.SaveChanges(); // Speichern, um die neue NCProgrammID zu erhalten

                // 3. Werkzeugeinsätze dem neuen Programm zuweisen
                foreach (var einsatz in werkzeugEinsaetze)
                {
                    einsatz.NCProgrammID = neuesProgramm.NCProgrammID;
                    context.WerkzeugEinsaetze.Add(einsatz);
                }
                context.SaveChanges();

                _mainViewModel.NavigateTo(new AnalysisViewModel(neuesProgramm, _mainViewModel));
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Erstellen eines neuen Projekts");
                MessageBox.Show($"Fehler beim Erstellen des Projekts:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- ANGEPASSTE METHODEN ZUM LADEN VON DATEN ---
        private void LoadStandorte()
        {
            // --- NEU: try-catch ---
            try
            {
                Standorte.Clear();
                using var context = new NcSetupContext();
                var standorteFromDb = context.Standorte
                                             .Include(s => s.Maschinen)
                                             .ToList();
                foreach (var standort in standorteFromDb)
                {
                    Standorte.Add(standort);
                }

                // NEU: Wenn es nur einen Standort gibt, wird dieser automatisch ausgewählt.
                if (Standorte.Count == 1)
                {
                    SelectedStandort = Standorte.First();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Standorte in NewProjectViewModel");
                MessageBox.Show($"Fehler beim Laden der Standorte:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMaschinen(Standort? standort)
        {
            Maschinen.Clear();
            SelectedMaschine = null; // Auswahl zurücksetzen
            if (standort != null)
            {
                // Die Maschinen sind bereits durch das Include geladen
                foreach (var maschine in standort.Maschinen)
                {
                    Maschinen.Add(maschine);
                }

                // NEU: Wenn es für den Standort nur eine Maschine gibt, wird diese automatisch ausgewählt.
                if (Maschinen.Count == 1)
                {
                    SelectedMaschine = Maschinen.First();
                }
            }
        }
    }
}