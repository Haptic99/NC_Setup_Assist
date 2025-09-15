// NC_Setup_Assist/ViewModels/NewProjectViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class NewProjectViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        // Listen für die ComboBoxen
        public ObservableCollection<Standort> Standorte { get; } = new();
        public ObservableCollection<Maschine> Maschinen { get; } = new();

        // Ausgewählte Elemente und Eingabefelder
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartAnalysisCommand))]
        private Standort? _selectedStandort;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartAnalysisCommand))]
        private Maschine? _selectedMaschine;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartAnalysisCommand))]
        private string? _ncFilePath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartAnalysisCommand))]
        private string? _bauteilBezeichnung;


        public NewProjectViewModel(MainViewModel mainViewModel)
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

        // Wird aufgerufen, wenn sich der Standort ändert
        partial void OnSelectedStandortChanged(Standort? value)
        {
            Maschinen.Clear();
            SelectedMaschine = null;
            if (value != null)
            {
                using var context = new NcSetupContext();
                var maschinenFromDb = context.Maschinen
                                            .Where(m => m.StandortID == value.StandortID)
                                            .ToList();
                foreach (var maschine in maschinenFromDb)
                {
                    Maschinen.Add(maschine);
                }
            }
        }

        [RelayCommand]
        private void SelectNcFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "NC Files (*.nc;*.min)|*.nc;*.min|All files (*.*)|*.*",
                Title = "NC-Programm auswählen"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                NcFilePath = openFileDialog.FileName;
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartAnalysis))]
        private void StartAnalysis()
        {
            // --- HIER WIRD SPÄTER DIE PARSER-LOGIK AUFGERUFEN ---

            // 1. Neues NCProgramm-Objekt erstellen und speichern
            var newNcProgram = new NCProgramm
            {
                DateiPfad = NcFilePath!,
                Bezeichnung = BauteilBezeichnung!,
                MaschineID = SelectedMaschine!.MaschineID,
                ZeichnungsNummer = "N/A" // Vorerst Platzhalter
            };

            using (var context = new NcSetupContext())
            {
                context.Maschinen.Attach(SelectedMaschine);
                context.NCProgramme.Add(newNcProgram);
                context.SaveChanges();

                // 2. Neues Projekt-Objekt erstellen und speichern
                var newProject = new Projekt
                {
                    AnalyseDatum = DateTime.Now,
                    NCProgrammID = newNcProgram.NCProgrammID
                };
                context.Projekte.Add(newProject);
                context.SaveChanges();
            }

            // 3. Zur Analyse-Ansicht navigieren (diese erstellen wir im nächsten Schritt)
            MessageBox.Show($"Projekt für '{newNcProgram.Bezeichnung}' wurde erstellt!\nNächster Schritt: Navigation zur Analyse-Ansicht.", "Erfolg");
            // _mainViewModel.NavigateTo(new AnalysisViewModel(newNcProgram)); // <-- Dies wird das endgültige Ziel sein
        }

        private bool CanStartAnalysis()
        {
            return !string.IsNullOrWhiteSpace(NcFilePath) &&
                   !string.IsNullOrWhiteSpace(BauteilBezeichnung) &&
                   SelectedMaschine != null;
        }
    }
}