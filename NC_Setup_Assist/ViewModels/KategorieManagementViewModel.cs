// NC_Setup_Assist/ViewModels/KategorieManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.Services; // <-- NEU
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class KategorieManagementViewModel : ViewModelBase
    {
        private readonly Action? _onDataChangedCallback;

        public ObservableCollection<WerkzeugKategorie> Kategorien { get; } = new();

        [ObservableProperty]
        private string? _newKategorieName;

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorie;

        public KategorieManagementViewModel(Action? onDataChangedCallback = null)
        {
            _onDataChangedCallback = onDataChangedCallback;
            LoadKategorien();
        }

        private void LoadKategorien()
        {
            // --- NEU: try-catch ---
            try
            {
                Kategorien.Clear();
                using var context = new NcSetupContext();
                var katsFromDb = context.WerkzeugKategorien.OrderBy(k => k.Name).ToList();
                foreach (var kat in katsFromDb)
                {
                    Kategorien.Add(kat);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Laden der Werkzeugkategorien");
                MessageBox.Show($"Fehler beim Laden der Kategorien:\n{ex.Message}", "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddKategorie()
        {
            if (string.IsNullOrWhiteSpace(NewKategorieName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen für die Kategorie ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- NEU: try-catch ---
            try
            {
                using (var context = new NcSetupContext())
                {
                    var newKat = new WerkzeugKategorie { Name = NewKategorieName };
                    context.WerkzeugKategorien.Add(newKat);
                    context.SaveChanges();
                }

                NewKategorieName = string.Empty;
                LoadKategorien();
                _onDataChangedCallback?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Hinzufügen einer Kategorie");
                MessageBox.Show($"Fehler beim Speichern der Kategorie:\n{ex.Message}", "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteKategorie()
        {
            if (SelectedKategorie == null) return;

            // --- NEU: try-catch ---
            try
            {
                // --- NEUE PRÜFUNG ---
                if (SelectedKategorie.WerkzeugKategorieID <= 3)
                {
                    MessageBox.Show($"Die Kategorie '{SelectedKategorie.Name}' ist eine Standardkategorie und kann nicht gelöscht werden.",
                                    "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // --- ENDE PRÜFUNG ---

                using (var context = new NcSetupContext())
                {
                    bool isUsed = context.WerkzeugUnterkategorien.Any(u => u.WerkzeugKategorieID == SelectedKategorie.WerkzeugKategorieID);
                    if (isUsed)
                    {
                        MessageBox.Show($"Die Kategorie '{SelectedKategorie.Name}' kann nicht gelöscht werden, da ihr noch Unterkategorien zugewiesen sind.",
                                        "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var result = MessageBox.Show($"Möchten Sie die Kategorie '{SelectedKategorie.Name}' wirklich löschen?",
                                             "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new NcSetupContext())
                    {
                        var katToDelete = context.WerkzeugKategorien.Find(SelectedKategorie.WerkzeugKategorieID);
                        if (katToDelete != null)
                        {
                            context.WerkzeugKategorien.Remove(katToDelete);
                            context.SaveChanges();
                        }
                    }
                    LoadKategorien();
                    _onDataChangedCallback?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "Fehler beim Löschen einer Kategorie");
                MessageBox.Show($"Fehler beim Löschen der Kategorie:\n{ex.Message}", "Löschfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}