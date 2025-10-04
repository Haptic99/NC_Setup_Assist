using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services;
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
        private string? _ncFilePath;

        [ObservableProperty]
        private string? _projectName;

        // --- NEUE EIGENSCHAFTEN ---
        public ObservableCollection<Firma> Firmen { get; } = new();
        public ObservableCollection<Maschine> Maschinen { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private Firma? _selectedFirma;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
        private Maschine? _selectedMaschine;


        public NewProjectViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadFirmen(); // Lädt die Firmenliste beim Start
        }

        // --- NEUE METHODE: Wird aufgerufen, wenn sich die Auswahl der Firma ändert ---
        partial void OnSelectedFirmaChanged(Firma? value)
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
                   SelectedFirma != null &&
                   SelectedMaschine != null;
        }

        [RelayCommand(CanExecute = nameof(CanCreateProject))]
        private void CreateProject()
        {
            if (string.IsNullOrWhiteSpace(NcFilePath) ||
                string.IsNullOrWhiteSpace(ProjectName) ||
                SelectedFirma == null ||
                SelectedMaschine == null)
            {
                MessageBox.Show("Bitte füllen Sie alle Felder aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parser = new NcCodeParserService();
            var werkzeugEinsaetze = parser.Parse(NcFilePath);

            using var context = new NcSetupContext();

            // Das neue Projekt wird mit der ID der ausgewählten Maschine erstellt
            var neuesProjekt = new Projekt
            {
                Name = ProjectName,
                MaschineID = SelectedMaschine.MaschineID // Korrigierte Zuweisung
            };
            context.Projekte.Add(neuesProjekt);
            context.SaveChanges(); // Speichern, um die neue ProjektID zu erhalten

            // Jetzt das NCProgramm erstellen und mit dem Projekt verknüpfen
            var neuesProgramm = new NCProgramm
            {
                // ZeichnungsNummer ist in Modell nicht nullable — also befüllen, z.B. mit dem Projektnamen oder Dateiname
                ZeichnungsNummer = ProjectName ?? Path.GetFileNameWithoutExtension(NcFilePath),
                Bezeichnung = Path.GetFileName(NcFilePath),
                DateiPfad = NcFilePath,
                MaschineID = SelectedMaschine.MaschineID // MaschineID zuweisen
            };
            context.NCProgramme.Add(neuesProgramm);
            context.SaveChanges(); // Speichern, um die neue NCProgrammID zu erhalten

            // Werkzeugeinsätze dem neuen Programm zuweisen
            foreach (var einsatz in werkzeugEinsaetze)
            {
                einsatz.NCProgrammID = neuesProgramm.NCProgrammID;
                context.WerkzeugEinsaetze.Add(einsatz);
            }
            context.SaveChanges();

            _mainViewModel.NavigateTo(new AnalysisViewModel(neuesProgramm, _mainViewModel));
        }

        // --- NEUE METHODEN ZUM LADEN VON DATEN ---
        private void LoadFirmen()
        {
            Firmen.Clear();
            using var context = new NcSetupContext();
            var firmenFromDb = context.Firmen
                                      .Include(f => f.Standorte)
                                      .ThenInclude(s => s.Maschinen)
                                      .ToList();
            foreach (var firma in firmenFromDb)
            {
                Firmen.Add(firma);
            }
        }

        private void LoadMaschinen(Firma? firma)
        {
            Maschinen.Clear();
            SelectedMaschine = null; // Auswahl zurücksetzen
            if (firma != null)
            {
                var maschinen = firma.Standorte.SelectMany(s => s.Maschinen).ToList();
                foreach (var maschine in maschinen)
                {
                    Maschinen.Add(maschine);
                }
            }
        }
    }
}