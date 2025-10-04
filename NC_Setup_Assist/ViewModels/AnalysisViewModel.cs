using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NC_Setup_Assist.ViewModels
{
    public partial class AnalysisViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly NCProgramm _currentProgramm;

        [ObservableProperty]
        private string? _ncCodeContent;

        public ObservableCollection<WerkzeugEinsatz> WerkzeugEinsaetze { get; } = new();

        public ObservableCollection<Firma> Firmen { get; } = new();
        public ObservableCollection<Maschine> Maschinen { get; } = new();

        [ObservableProperty]
        private Firma? _selectedFirma;

        [ObservableProperty]
        private Maschine? _selectedMaschine;

        // --- NEUES FELD ZUM SPEICHERN DER LETZTEN SUCHE ---
        private readonly Dictionary<int, int> _lastSearchIndex = new();

        // --- NEUES EREIGNIS, UM DIE ANSICHT ZU BENACHRICHTIGEN ---
        public event Action<int, int>? RequestScrollAndSelect;

        public AnalysisViewModel(NCProgramm ncProgramm, MainViewModel mainViewModel)
        {
            _currentProgramm = ncProgramm;
            _mainViewModel = mainViewModel;

            LoadNcFileContent();
            LoadWerkzeugEinsaetze();
            LoadFirmen();
        }

        partial void OnSelectedFirmaChanged(Firma? value)
        {
            LoadMaschinen(value);
        }

        // --- NEUER BEFEHL FÜR DEN DOPPELKLICK ---
        [RelayCommand]
        private void GoToToolInCode(WerkzeugEinsatz? werkzeugEinsatz)
        {
            if (werkzeugEinsatz == null || string.IsNullOrEmpty(NcCodeContent))
            {
                return;
            }

            // Erzeugt einen eindeutigen String für das Werkzeug, z.B. "T0101"
            string toolIdentifier = $"T{werkzeugEinsatz.RevolverStation:D2}{werkzeugEinsatz.KorrekturNummer:D2}";

            int startIndex = 0;
            if (_lastSearchIndex.ContainsKey(werkzeugEinsatz.WerkzeugEinsatzID))
            {
                startIndex = _lastSearchIndex[werkzeugEinsatz.WerkzeugEinsatzID] + 1;
            }

            int foundIndex = NcCodeContent.IndexOf(toolIdentifier, startIndex, StringComparison.OrdinalIgnoreCase);

            // Wenn nichts gefunden wurde, fangen Sie von vorne an
            if (foundIndex == -1)
            {
                _lastSearchIndex.Remove(werkzeugEinsatz.WerkzeugEinsatzID);
                foundIndex = NcCodeContent.IndexOf(toolIdentifier, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (foundIndex != -1)
            {
                _lastSearchIndex[werkzeugEinsatz.WerkzeugEinsatzID] = foundIndex;

                // Löst das Ereignis aus, um die Ansicht zu benachrichtigen
                RequestScrollAndSelect?.Invoke(foundIndex, toolIdentifier.Length);
            }
        }

        private void LoadNcFileContent()
        {
            // Pfad aus der Benutzereingabe verwenden, wenn die Datei nicht am ursprünglichen Ort ist
            string filePath = _currentProgramm.DateiPfad;
            if (!File.Exists(filePath))
            {
                filePath = @"C:\Users\dmart\OneDrive\Arbeit\Firmen\STB Maschinenbau AG\Einrichtblatt Dreherei\Automatisch Einrichtblatt Dreherei\Programme\SC02312006.nc";
            }

            if (File.Exists(filePath))
            {
                NcCodeContent = File.ReadAllText(filePath);
            }
            else
            {
                NcCodeContent = $"Fehler: Die Datei konnte in keinem der Pfade gefunden werden:\n1. {_currentProgramm.DateiPfad}\n2. {filePath}";
            }
        }

        private void LoadWerkzeugEinsaetze()
        {
            WerkzeugEinsaetze.Clear();
            using var context = new NcSetupContext();

            var einsaetzeFromDb = context.WerkzeugEinsaetze
                                        .Where(e => e.NCProgrammID == _currentProgramm.NCProgrammID)
                                        .Include(e => e.ZugehoerigesWerkzeug)
                                        .OrderBy(e => e.Reihenfolge)
                                        .ToList();

            foreach (var einsatz in einsaetzeFromDb)
            {
                WerkzeugEinsaetze.Add(einsatz);
            }
        }

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
                // Sammelt alle Maschinen von allen Standorten der ausgewählten Firma
                var maschinen = firma.Standorte.SelectMany(s => s.Maschinen).ToList();
                foreach (var maschine in maschinen)
                {
                    Maschinen.Add(maschine);
                }
            }
        }
    }
}