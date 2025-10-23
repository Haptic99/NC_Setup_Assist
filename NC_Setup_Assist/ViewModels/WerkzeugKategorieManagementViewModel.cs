// NC_Setup_Assist/ViewModels/WerkzeugKategorieManagementViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NC_Setup_Assist.ViewModels
{
    public partial class WerkzeugKategorieManagementViewModel : ViewModelBase
    {
        private readonly Action? _onDataChangedCallback;

        // Listen für die UI
        public ObservableCollection<WerkzeugKategorie> Kategorien { get; } = new();
        public ObservableCollection<WerkzeugUnterkategorie> FilteredUnterkategorien { get; } = new();

        // --- Sektion Hauptkategorien ---
        [ObservableProperty]
        private string? _newKategorieName;

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorie;

        // --- Sektion Unterkategorien ---
        [ObservableProperty]
        private string? _newUnterkategorieName;

        [ObservableProperty]
        private WerkzeugUnterkategorie? _selectedUnterkategorie;

        [ObservableProperty]
        private WerkzeugKategorie? _selectedKategorieForSub; // Kategorie-Auswahl im rechten Panel

        public WerkzeugKategorieManagementViewModel(Action? onDataChangedCallback = null)
        {
            _onDataChangedCallback = onDataChangedCallback;
            LoadKategorien();
        }

        private void LoadKategorien()
        {
            Kategorien.Clear();
            using var context = new NcSetupContext();
            var katsFromDb = context.WerkzeugKategorien.OrderBy(k => k.Name).ToList();
            foreach (var kat in katsFromDb)
            {
                Kategorien.Add(kat);
            }
        }

        // Lädt die Unterkategorien basierend auf der Auswahl im rechten Panel
        partial void OnSelectedKategorieForSubChanged(WerkzeugKategorie? value)
        {
            FilteredUnterkategorien.Clear();
            SelectedUnterkategorie = null;

            if (value != null)
            {
                using var context = new NcSetupContext();
                var subsFromDb = context.WerkzeugUnterkategorien
                                        .Where(u => u.WerkzeugKategorieID == value.WerkzeugKategorieID)
                                        .OrderBy(u => u.Name)
                                        .ToList();
                foreach (var sub in subsFromDb)
                {
                    FilteredUnterkategorien.Add(sub);
                }
            }
        }

        #region Kategorie Commands

        [RelayCommand]
        private void AddKategorie()
        {
            if (string.IsNullOrWhiteSpace(NewKategorieName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen für die Kategorie ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

        [RelayCommand]
        private void DeleteKategorie()
        {
            if (SelectedKategorie == null) return;

            using (var context = new NcSetupContext())
            {
                // Prüfen, ob die Kategorie noch verwendet wird
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

        #endregion

        #region Unterkategorie Commands

        [RelayCommand]
        private void AddUnterkategorie()
        {
            if (SelectedKategorieForSub == null)
            {
                MessageBox.Show("Bitte wählen Sie zuerst eine Hauptkategorie aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewUnterkategorieName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen für die Unterkategorie ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new NcSetupContext())
            {
                var newSubKat = new WerkzeugUnterkategorie
                {
                    Name = NewUnterkategorieName,
                    WerkzeugKategorieID = SelectedKategorieForSub.WerkzeugKategorieID
                };
                context.WerkzeugUnterkategorien.Add(newSubKat);
                context.SaveChanges();
            }

            NewUnterkategorieName = string.Empty;
            // Lade die Liste neu
            OnSelectedKategorieForSubChanged(SelectedKategorieForSub);
            _onDataChangedCallback?.Invoke();
        }

        [RelayCommand]
        private void DeleteUnterkategorie()
        {
            if (SelectedUnterkategorie == null) return;

            using (var context = new NcSetupContext())
            {
                // Prüfen, ob die Unterkategorie noch verwendet wird
                bool isUsed = context.Werkzeuge.Any(w => w.WerkzeugUnterkategorieID == SelectedUnterkategorie.WerkzeugUnterkategorieID);
                if (isUsed)
                {
                    MessageBox.Show($"Die Unterkategorie '{SelectedUnterkategorie.Name}' kann nicht gelöscht werden, da sie noch Werkzeugen zugewiesen ist.",
                                    "Löschen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show($"Möchten Sie die Unterkategorie '{SelectedUnterkategorie.Name}' wirklich löschen?",
                                         "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new NcSetupContext())
                {
                    var subKatToDelete = context.WerkzeugUnterkategorien.Find(SelectedUnterkategorie.WerkzeugUnterkategorieID);
                    if (subKatToDelete != null)
                    {
                        context.WerkzeugUnterkategorien.Remove(subKatToDelete);
                        context.SaveChanges();
                    }
                }
                // Lade die Liste neu
                OnSelectedKategorieForSubChanged(SelectedKategorieForSub);
                _onDataChangedCallback?.Invoke();
            }
        }

        #endregion
    }
}