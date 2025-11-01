// NC_Setup_Assist/ViewModels/HerstellerManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services; // <-- NEU
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class HerstellerManagementViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Hersteller> _hersteller = new();

        [ObservableProperty]
        private Hersteller? _selectedHersteller;

        [ObservableProperty]
        private string? _newHerstellerName;

        // --- NEU: Callback, um den Aufrufer über Änderungen zu informieren ---
        private readonly Action? _onDataChangedCallback;

        public HerstellerManagementViewModel(Action? onDataChangedCallback = null)
        {
            _onDataChangedCallback = onDataChangedCallback;
            LoadHersteller();
        }

        private void LoadHersteller()
        {
            // --- NEU: try-catch ---
            try
            {
                Hersteller.Clear();
                using var context = new NcSetupContext();
                var herstellerFromDb = context.Hersteller.ToList();
                foreach (var hersteller in herstellerFromDb)
                {
                    Hersteller.Add(hersteller);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Hersteller");
                MessageBox.Show($"Fehler beim Laden der Hersteller:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddHersteller()
        {
            if (string.IsNullOrWhiteSpace(NewHerstellerName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen für den Hersteller ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- NEU: try-catch ---
            try
            {
                using (var context = new NcSetupContext())
                {
                    var newHersteller = new Hersteller { Name = NewHerstellerName };
                    context.Hersteller.Add(newHersteller);
                    context.SaveChanges();
                }

                NewHerstellerName = string.Empty;
                LoadHersteller();

                // --- NEU: Aufrufer benachrichtigen ---
                _onDataChangedCallback?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Hinzufügen eines Herstellers");
                MessageBox.Show($"Fehler beim Speichern des Herstellers:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteHersteller()
        {
            if (SelectedHersteller == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Hersteller zum Löschen aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- NEU: try-catch ---
            try
            {
                using (var context = new NcSetupContext())
                {
                    // PRÜFUNG: Wird der Hersteller noch verwendet?
                    bool isUsed = context.Maschinen.Any(m => m.HerstellerID == SelectedHersteller.HerstellerID);
                    if (isUsed)
                    {
                        MessageBox.Show($"Der Hersteller '{SelectedHersteller.Name}' kann nicht gelöscht werden, da er noch von mindestens einer Maschine verwendet wird.",
                                        "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // Methode hier beenden
                    }
                }

                var result = MessageBox.Show($"Möchten Sie den Hersteller '{SelectedHersteller.Name}' wirklich löschen?",
                                             "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new NcSetupContext())
                    {
                        var herstellerToDelete = context.Hersteller.Find(SelectedHersteller.HerstellerID);
                        if (herstellerToDelete != null)
                        {
                            context.Hersteller.Remove(herstellerToDelete);
                            context.SaveChanges();
                        }
                    }
                    LoadHersteller();
                    _onDataChangedCallback?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Löschen eines Herstellers");
                MessageBox.Show($"Fehler beim Löschen des Herstellers:\n{ex.Message}", "Löschfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}